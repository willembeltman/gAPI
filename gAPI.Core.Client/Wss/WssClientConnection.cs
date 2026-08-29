using gAPI.Core.Client.Config;
using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace gAPI.Core.Client.Wss;

public abstract class WssClientConnection : IWssClientConnection
{
    public WssClientConnection(
        IClientAuthenticatedHttpClient httpClient,
        string wssBackendUrl)
    {
        HttpClient = httpClient;
        HttpClient.OnStateHasChanged += HttpClient_OnStateHasChanged;
        WssBackendUrl = wssBackendUrl;
        Logger = ((IWssLoggerFactory)this).CreateLogger<WssClientConnection>();
    }

    private readonly ILogger Logger;
    private readonly SemaphoreSlim InitLock = new(1, 1);
    private readonly ConcurrentDictionary<string, SubscribeDto> Subscriptions = [];
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Func<CancellationToken, Task>> ArgumentRequestHandlers = [];
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Action<InvokeArgumentResponseDto>> ArgumentResponseHandlers = [];
    private byte[] ReceiveBuffer = new byte[10 * 1024 * 1024];
    private byte[] SendBuffer = new byte[10 * 1024 * 1024];
    private readonly Channel<Func<Span<byte>, int>> SendQueue = Channel.CreateUnbounded<Func<Span<byte>, int>>();

    private readonly string WssBackendUrl;
    private Task? InitializeTask;
    private ClientWebSocket? Ws;

    protected readonly IClientAuthenticatedHttpClient HttpClient;
    protected CancellationTokenSource? Cts;

    public bool Initialized { get; private set; }

    public bool IsConnected => Ws?.State == WebSocketState.Open;
    public SessionId SessionId => HttpClient.SessionId;

    // TODO uitzoeken of dit naar behoren werkt.
    private void HttpClient_OnStateHasChanged()
    {
        if (HttpClient.ForceReconnect)
        {
            HttpClient.ForceReconnect = false;
            _ = ForceReconnectAsync(new());
        }
    }

    public async Task TryConnectAsync(CancellationToken ct)
    {
        await InitLock.WaitAsync(ct);
        try
        {
            if (IsConnected)
                return;

            InitializeTask ??= ConnectAsync(WssBackendUrl, ct);
        }
        finally
        {
            InitLock.Release();
        }

        await InitializeTask;
    }
    private async Task ConnectAsync(string baseUri, CancellationToken ct)
    {
        try
        {
            var stateData = await HttpClient.GetStateDataAsync(false, ct);
            var sessionId = HttpClient.SessionId.Value;

            Cts = new();
            Ws = new ClientWebSocket();
            var url = new Uri($"{baseUri}/fabricr?SessionId={sessionId}");
            await Ws.ConnectAsync(url, ct); // baseUri = {https://localhost:7117/}

            _ = Task.Run(async () => { await ReceiverKernel(Ws, Cts); }, Cts.Token);
            _ = Task.Run(async () => { await SendKernel(Ws, Cts.Token); }, Cts.Token);

            var initialize = new InitializeDto()
            {
                SessionId = sessionId,
                StateData = stateData,
            };
            await Send_Initialize_ToServerAsync(initialize, Cts.Token);

            Initialized = true;
        }
        catch (Exception ex) // ex = {"net_webstatus_ConnectFailure"}
        {
            Logger.LogError("ConnectAsync => Exception: {ex}", ex);

            await InitLock.WaitAsync(ct);
            InitializeTask = null;
            InitLock.Release();
            throw;
        }
    }

    public async Task ForceReconnectAsync(CancellationToken ct)
    {
        if (HttpClient.BaseUri == null)
            throw new Exception("Cannot get base url from IClientAuthenticatedHttpClient");

        await InitLock.WaitAsync(ct);
        try
        {
            // 1. Stop bestaande kernels
            try
            {
                Cts?.Cancel();
            }
            catch { }

            // 2. Sluit websocket netjes
            if (Ws != null)
            {
                try
                {
                    if (Ws.State == WebSocketState.Open ||
                        Ws.State == WebSocketState.CloseReceived)
                    {
                        await Ws.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Force reconnect",
                            CancellationToken.None);
                    }
                }
                catch { }

                Ws.Dispose();
                Ws = null!;
            }

            // 3. Reset state
            Initialized = false;
            InitializeTask = null;

            // 4. Nieuwe CTS maken
            Cts?.Dispose();
            Cts = new CancellationTokenSource();
        }
        finally
        {
            InitLock.Release();
        }

        // 5. Opnieuw verbinden via bestaande flow
        await TryConnectAsync(ct);
    }

    private async Task ReceiverKernel(WebSocket socket, CancellationTokenSource cts)
    {
        var ct = cts.Token;

        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                int totalBytes = 0;
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(ReceiveBuffer, totalBytes, ReceiveBuffer.Length - totalBytes),
                        ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                        await cts.CancelAsync();
                        return;
                    }

                    totalBytes += result.Count;

                } while (!result.EndOfMessage);

                // 🎯 Direct span gebruiken
                var span = new ReadOnlySpan<byte>(ReceiveBuffer, 0, totalBytes);
                int offset = 0;

                var messageType = span.ReadWssServerToClientMessageEnum(ref offset);

                switch (messageType)
                {
                    case WssServerToClientMessageEnum.SendRequest:
                        var sendRequest = span.ReadSendRequestDto(ref offset);
                        await HttpClient.UpdateStateDataAsync(sendRequest.StateData, ct);
                        await Received_SendRequest_FromServerAsync(sendRequest, ct);
                        break;

                    case WssServerToClientMessageEnum.SendArgumentedRequest:
                        var sendArgumentedRequest = span.ReadSendRequestDto(ref offset);
                        await HttpClient.UpdateStateDataAsync(sendArgumentedRequest.StateData, ct);
                        _ = Task.Run(async () =>
                        {
                            await Received_SendArgumentedRequest_FromServerAsync(sendArgumentedRequest, ct);
                            await Send_SendArgumentedRequestDone_ToServerAsync(sendArgumentedRequest.RequestId, ct);
                        }, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeRequest:
                        var invokeRequest = span.ReadInvokeRequestDto(ref offset);
                        await HttpClient.UpdateStateDataAsync(invokeRequest.StateData, ct);
                        _ = Task.Run(async () => { await Received_InvokeRequest_FromServerAsync(invokeRequest, ct); }, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeArgumentRequest:
                        var argumentRequest = span.ReadInvokeArgumentRequestDto(ref offset);
                        _ = Task.Run(async () => { await Received_InvokeArgumentRequest_FromServerAsync(argumentRequest, ct); }, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeArgumentResponse:
                        var argumentResponse = span.ReadInvokeArgumentResponseDto(ref offset);
                        if (ArgumentResponseHandlers.TryGetValue((argumentResponse.RequestId, argumentResponse.ArgumentIndex), out var responseHandler))
                            responseHandler(argumentResponse);
                        break;

                    case WssServerToClientMessageEnum.InvokeResponse:
                        var invokeResponse = span.ReadApiInvokeResponseDto(ref offset);
                        await HttpClient.UpdateStateDataAsync(invokeResponse.StateData, ct);
                        await Received_InvokeResponse_FromServerAsync(invokeResponse, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeResponseDone:
                        var invokeResponseDone = span.ReadApiInvokeResponseDoneDto(ref offset);
                        await HttpClient.UpdateStateDataAsync(invokeResponseDone.StateData, ct);
                        await Received_InvokeResponseDone_FromServerAsync(invokeResponseDone, ct);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("ReceiverKernel => Exception: {ex}", ex);
            cts.Cancel();
            cts.Dispose();
            throw;
        }
    }

    private async Task Received_InvokeArgumentRequest_FromServerAsync(InvokeArgumentRequestDto argumentRequest, CancellationToken ct)
    {
        if (ArgumentRequestHandlers.TryGetValue((argumentRequest.RequestId, argumentRequest.ArgumentIndex), out var argumentHandler))
            await argumentHandler(ct);
    }

    protected abstract Task Received_SendRequest_FromServerAsync(SendRequestDto sendRequest, CancellationToken ct);
    protected abstract Task Received_SendArgumentedRequest_FromServerAsync(SendRequestDto sendRequest, CancellationToken ct);
    protected abstract Task Received_InvokeRequest_FromServerAsync(InvokeRequestDto invokeRequest, CancellationToken ct);
    protected abstract Task Received_InvokeResponse_FromServerAsync(ApiInvokeResponseDto invokeResponse, CancellationToken ct);
    protected abstract Task Received_InvokeResponseDone_FromServerAsync(ApiInvokeResponseDoneDto invokeResponseDone, CancellationToken ct);

    private async Task Send_Initialize_ToServerAsync(InitializeDto initialize, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("SendRequestAsync({initialize})", initialize);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Initialize);
            writer.Write(ref offset, initialize);
            return offset;
        }, ct);
    }

    public async Task Send_Subscribe_ToServerAsync(SubscribeDto subscribe, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Subscribe_ToServerAsync({subscribe})", subscribe);

        Subscriptions[subscribe.ToString()] = subscribe;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Subscribe);
            writer.Write(ref offset, subscribe);
            return offset;
        }, ct);
    }
    public async Task Send_Unsubscribe_ToServerAsync(UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Unsubscribe_ToServerAsync({unsubscribe})", unsubscribe);

        Subscriptions.Remove(unsubscribe.ToString(), out _);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Unsubscribe);
            writer.Write(ref offset, unsubscribe);
            return offset;
        }, ct);
    }

    public async Task Send_SendRequest_ToServerAsync(ApiSendRequestDto sendRequest, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("SendRequestAsync({sendRequest})", sendRequest);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequest);
            writer.Write(ref offset, sendRequest);
            return offset;
        }, ct);
    }
    public async Task Send_SendArgumentedRequest_ToServerAsync(ApiSendRequestDto sendRequest, CancellationToken ct)
    {
        if (!Initialized)
            return;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendArgumentedRequest);
            writer.Write(ref offset, sendRequest);
            return offset;
        }, ct);
    }

    public async Task Send_SendArgumentedRequestDone_ToServerAsync(RequestId requestId, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendArgumentedRequestDone);
            writer.Write(ref offset, new SendArgumentedRequestDoneDto { RequestId = requestId });
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequest_ToServerAsync(ApiInvokeRequestDto invokeRequest, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("InvokeRequestAsync({invokeRequest})", invokeRequest);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeRequest);
            writer.Write(ref offset, invokeRequest);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeResponse_ToServerAsync(InvokeResponseDto invokeResponse, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("InvokeResponseAsync({invokeResponse})", invokeResponse);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeResponse);
            writer.Write(ref offset, invokeResponse);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeResponseDone_ToServerAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("InvokeResponseDoneAsync({invokeResponseDone})", invokeResponseDone);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeResponseDone);
            writer.Write(ref offset, invokeResponseDone);
            return offset;
        }, ct);
    }

    public void RegisterAsyncEnumerableArgument<T>(RequestId requestId, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        var gate = new SemaphoreSlim(1, 1);
        var timeout = new ResettableTimeout(TimeSpan.FromSeconds(60), () =>
        {
            ArgumentRequestHandlers.TryRemove((requestId, argumentIndex), out _);
            _ = enumerator.DisposeAsync().AsTask();
        });
        ArgumentRequestHandlers[(requestId, argumentIndex)] = async ct =>
        {
            timeout.Reset();
            await gate.WaitAsync(ct);
            try
            {
                var hasNext = await enumerator.MoveNextAsync();
                await Send_InvokeArgumentResponse_ToServerAsync(new InvokeArgumentResponseDto
                {
                    RequestId = requestId,
                    ArgumentIndex = argumentIndex,
                    IsCompleted = !hasNext,
                    BinaryData = hasNext ? serializer(enumerator.Current) : []
                }, ct);
                if (!hasNext)
                {
                    ArgumentRequestHandlers.TryRemove((requestId, argumentIndex), out _);
                    timeout.Dispose();
                    await enumerator.DisposeAsync();
                }
            }
            finally
            {
                gate.Release();
            }
        };
    }
    public IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RequestId requestId, int argumentIndex, Func<byte[], T> deserializer)
    {
        var remote = new RemoteAsyncEnumerable<T>(ct => Send_InvokeArgumentRequest_ToServerAsync(new InvokeArgumentRequestDto
        {
            RequestId = requestId,
            ArgumentIndex = argumentIndex
        }, ct));
        ArgumentResponseHandlers[(requestId, argumentIndex)] = response =>
        {
            if (response.IsCompleted)
            {
                ArgumentResponseHandlers.TryRemove((requestId, argumentIndex), out _);
                remote.Complete();
            }
            else
                remote.Push(deserializer(response.BinaryData));
        };
        return remote;
    }
    private async Task Send_InvokeArgumentRequest_ToServerAsync(InvokeArgumentRequestDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeArgumentRequest);
            writer.Write(ref offset, request);
            return offset;
        }, ct);
    }
    private async Task Send_InvokeArgumentResponse_ToServerAsync(InvokeArgumentResponseDto response, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeArgumentResponse);
            writer.Write(ref offset, response);
            return offset;
        }, ct);
    }

    public async Task Send_Log_ToServerAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        Console.WriteLine(log);
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Log);
            writer.Write(ref offset, log);
            return offset;
        }, ct);
    }

    private async Task EnqueueAsync(Func<Span<byte>, int> write, CancellationToken ct)
    {
        try
        {
            await SendQueue.Writer.WriteAsync(write, ct);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task SendKernel(WebSocket socket, CancellationToken ct)
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
        {
            var span = SendBuffer.AsSpan();

            // 🚀 direct serializen in pooled buffer
            var offset = item(span);

            // 🚀 direct versturen zonder kopie
            await socket.SendAsync(
                new ArraySegment<byte>(SendBuffer, 0, offset),
                WebSocketMessageType.Binary,
                true,
                ct);
        }
    }

    public ILogger CreateLogger(string categoryName)
        => new WssLogger(categoryName, this);
    public void AddProvider(ILoggerProvider provider)
    {
        // no-op
    }

    public void Dispose()
    {
        HttpClient.OnStateHasChanged -= HttpClient_OnStateHasChanged;
        Cts?.Cancel();
        Cts?.Dispose();
        Ws?.Dispose();
        GC.SuppressFinalize(this);
    }
}