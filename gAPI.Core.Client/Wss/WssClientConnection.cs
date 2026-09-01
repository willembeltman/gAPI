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
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Func<Guid, CancellationToken, Task>> ArgumentRequestHandlers = [];
    private readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex, Guid StreamId), Action<InvokeArgumentResponseDto>> ArgumentResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<bool>> PendingArgumentedRequests = [];
    private readonly ConcurrentDictionary<RequestId, Channel<InvokeResponseDto>> PendingInvokeRequests = [];
    private readonly ConcurrentDictionary<RequestId, ResettableTimeout> InvokeTimeouts = [];
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

    protected abstract Task Send_SendRequest_ToServiceAsync(SendRequestDto sendRequest, CancellationToken ct);
    protected abstract Task Send_InvokeRequest_ToServiceAsync(InvokeRequestDto invokeRequest, CancellationToken ct);

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
        while (true)
        {
            try
            {
                var stateData = await HttpClient.GetStateDataAsync(false, ct);
                var sessionId = HttpClient.SessionId.Value;

                Cts = new();
                Ws = new ClientWebSocket();
                var url = new Uri($"{baseUri}/fabricr?SessionId={sessionId}");
                await Ws.ConnectAsync(url, ct);

                _ = Task.Run(async () => { await ReceiverKernel(Ws, Cts); }, Cts.Token);
                _ = Task.Run(async () => { await SendKernel(Ws, Cts.Token); }, Cts.Token);

                var initialize = new InitializeDto()
                {
                    StateData = stateData,
                };
                await Send_Initialize_ToServerAsync(initialize, Cts.Token);

                Initialized = true;
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Initialized = false;
                Logger.LogWarning("ConnectAsync => connection failed, retrying: {ex}", ex.Message);

                try
                {
                    Cts?.Cancel();
                    Ws?.Dispose();
                }
                catch
                {
                }

                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
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
                        var sendArgumentedRequest = span.ReadSendRequestDto(ref offset);
                        _ = Task.Run(async () => { await Received_SendRequest_FromServer(sendArgumentedRequest, ct); }, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeRequest:
                        var invokeRequest = span.ReadInvokeRequestDto(ref offset);
                        _ = Task.Run(async () => { await Received_InvokeRequest_FromServerAsync(invokeRequest, ct); }, ct);
                        break;

                    case WssServerToClientMessageEnum.SendRequestDone:
                        var sendArgumentedRequestDone = span.ReadSendRequestDoneDto(ref offset);
                        await Received_SendRequestDone_FromServer(sendArgumentedRequestDone, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeArgumentRequest:
                        var argumentRequest = span.ReadInvokeArgumentRequestDto(ref offset);
                        _ = Task.Run(async () => { await Received_InvokeArgumentRequest_FromServerAsync(argumentRequest, ct); }, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeArgumentResponse:
                        var argumentResponse = span.ReadInvokeArgumentResponseDto(ref offset);
                        await Received_InvokeArgumentResponse_FromServer(argumentResponse, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeResponse:
                        var invokeResponse = span.ReadInvokeResponseDto(ref offset);
                        await Received_InvokeResponse_FromServerAsync(invokeResponse, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeResponseDone:
                        var invokeResponseDone = span.ReadInvokeResponseDoneDto(ref offset);
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

    private async Task Received_InvokeArgumentResponse_FromServer(InvokeArgumentResponseDto argumentResponse, CancellationToken ct)
    {
        if (InvokeTimeouts.TryGetValue(argumentResponse.RequestId, out var timeout))
            timeout.Reset();

        if (ArgumentResponseHandlers.TryGetValue((argumentResponse.RequestId, argumentResponse.ArgumentIndex, argumentResponse.StreamId), out var responseHandler))
            responseHandler(argumentResponse);
    }
    private async Task Received_SendRequestDone_FromServer(SendRequestDoneDto sendArgumentedRequestDone, CancellationToken ct)
    {
        if (PendingArgumentedRequests.TryRemove(sendArgumentedRequestDone.RequestId, out var completion))
            completion.TrySetResult(true);
    }
    private async Task Received_SendRequest_FromServer(SendRequestDto sendArgumentedRequest, CancellationToken ct)
    {
        try
        {
            if (sendArgumentedRequest.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(sendArgumentedRequest.StateData, ct);
            await Send_SendRequest_ToServiceAsync(sendArgumentedRequest, ct);
            var stateIsChanged = HttpClient.IsStateDataChanged();
            await Send_SendRequestDone_ToServerAsync(
                new SendRequestDoneDto(
                    sendArgumentedRequest.RequestId,
                    sendArgumentedRequest.ServiceId,
                    sendArgumentedRequest.MethodId,
                    sendArgumentedRequest.UserId,
                    sendArgumentedRequest.SessionId,
                    stateIsChanged,
                    stateIsChanged ? await HttpClient.GetStateDataAsync() : null,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            var stateIsChanged = HttpClient.IsStateDataChanged();
            await Send_SendRequestDone_ToServerAsync(
                new SendRequestDoneDto(
                    sendArgumentedRequest.RequestId,
                    sendArgumentedRequest.ServiceId,
                    sendArgumentedRequest.MethodId,
                    sendArgumentedRequest.UserId,
                    sendArgumentedRequest.SessionId,
                    stateIsChanged,
                    stateIsChanged ? await HttpClient.GetStateDataAsync() : null,
                    ex.Message),
                ct);
        }
    }
    private async Task Received_InvokeRequest_FromServerAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
    {
        try
        {
            if (invokeRequest.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(invokeRequest.StateData, ct);
            await Send_InvokeRequest_ToServiceAsync(invokeRequest, ct);
            await Send_InvokeResponseDone_ToServerAsync(
                new InvokeResponseDoneDto(
                    invokeRequest.RequestId,
                    invokeRequest.ServiceId,
                    invokeRequest.MethodId,
                    invokeRequest.UserId,
                    invokeRequest.SessionId,
                    null), ct);
        }
        catch (Exception ex)
        {
            await Send_InvokeResponseDone_ToServerAsync(
                new InvokeResponseDoneDto(
                    invokeRequest.RequestId,
                    invokeRequest.ServiceId,
                    invokeRequest.MethodId,
                    invokeRequest.UserId,
                    invokeRequest.SessionId,
                    ex.Message), ct);
        }
    }
    private async Task Received_InvokeArgumentRequest_FromServerAsync(InvokeArgumentRequestDto argumentRequest, CancellationToken ct)
    {
        if (InvokeTimeouts.TryGetValue(argumentRequest.RequestId, out var timeout))
            timeout.Reset();

        if (ArgumentRequestHandlers.TryGetValue((argumentRequest.RequestId, argumentRequest.ArgumentIndex), out var argumentHandler))
            await argumentHandler(argumentRequest.StreamId, ct);
    }
    private async Task Received_InvokeResponse_FromServerAsync(InvokeResponseDto invokeResponse, CancellationToken ct)
    {
        //await HttpClient.UpdateStateDataAsync(invokeResponse.StateData, ct);
        if (InvokeTimeouts.TryGetValue(invokeResponse.RequestId, out var timeout))
            timeout.Reset();

        if (PendingInvokeRequests.TryGetValue(invokeResponse.RequestId, out var ___channel))
            ___channel.Writer.TryWrite(invokeResponse);
    }
    public async Task Received_InvokeResponseDone_FromServerAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken ct)
    {
        //await HttpClient.UpdateStateDataAsync(invokeResponseDone.StateData, ct);

        if (PendingInvokeRequests.TryRemove(invokeResponseDone.RequestId, out var ___channel))
            ___channel.Writer.TryComplete();

        UnregisterInvokeRequest(invokeResponseDone.RequestId);
    }

    public void RegisterInvokeRequest(RequestId requestId, Channel<InvokeResponseDto> channel)
    {
        PendingInvokeRequests[requestId] = channel;
        InvokeTimeouts[requestId] = new ResettableTimeout(TimeSpan.FromSeconds(60), () =>
        {
            if (PendingInvokeRequests.TryRemove(requestId, out var pending))
                pending.Writer.TryComplete(new TimeoutException("Invoke request timed out."));
            if (InvokeTimeouts.TryRemove(requestId, out var timeout))
                timeout.Dispose();
        });
    }
    public void UnregisterInvokeRequest(RequestId requestId)
    {
        PendingInvokeRequests.TryRemove(requestId, out _);
        if (InvokeTimeouts.TryRemove(requestId, out var timeout))
            timeout.Dispose();
    }

    //public async Task<T> InvokeAsync<T>(InvokeRequestDto request, Func<byte[], T> deserialize, CancellationToken ct)
    //{
    //    var channel = Channel.CreateUnbounded<InvokeResponseDto>();
    //    PendingInvokeRequests[request.RequestId] = channel;
    //    using var timeout = CreateInvokeTimeout(request.RequestId, channel);

    //    try
    //    {
    //        await Send_InvokeRequest_ToServerAsync(request, ct);
    //        T result = default!;
    //        await foreach (var response in channel.Reader.ReadAllAsync(ct))
    //            result = deserialize(response.BinaryData);
    //        return result;
    //    }
    //    finally
    //    {
    //        PendingInvokeRequests.TryRemove(request.RequestId, out _);
    //        channel.Writer.TryComplete();
    //    }
    //}
    //public async IAsyncEnumerable<T> InvokeStreamingAsync<T>(InvokeRequestDto request, Func<byte[], T> deserialize, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    //{
    //    var channel = Channel.CreateUnbounded<InvokeResponseDto>();
    //    PendingInvokeRequests[request.RequestId] = channel;
    //    using var timeout = CreateInvokeTimeout(request.RequestId, channel);

    //    try
    //    {
    //        await Send_InvokeRequest_ToServerAsync(request, ct);
    //        await foreach (var response in channel.Reader.ReadAllAsync(ct))
    //            yield return deserialize(response.BinaryData);
    //    }
    //    finally
    //    {
    //        PendingInvokeRequests.TryRemove(request.RequestId, out _);
    //        channel.Writer.TryComplete();
    //    }
    //}

    //private ResettableTimeout CreateInvokeTimeout(RequestId requestId, Channel<InvokeResponseDto> channel)
    //{
    //    return new ResettableTimeout(TimeSpan.FromSeconds(60), () =>
    //    {
    //        if (PendingInvokeRequests.TryRemove(requestId, out _))
    //            channel.Writer.TryComplete(new TimeoutException("Invoke request timed out."));
    //    });
    //}

    private async Task Send_Initialize_ToServerAsync(InitializeDto initialize, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequest_ToServiceAsync({initialize})", initialize);

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

    public async Task Send_SendRequest_ToServerAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        if (!Initialized)
            return;

        var completion = PendingArgumentedRequests.GetOrAdd(
            sendRequest.RequestId,
            _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequest);
            writer.Write(ref offset, sendRequest);
            return offset;
        }, ct);

        try
        {
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        finally
        {
            PendingArgumentedRequests.TryRemove(sendRequest.RequestId, out _);
        }
    }
    public async Task Send_SendRequestDone_ToServerAsync(SendRequestDoneDto sendRequestDone, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequestDone);
            writer.Write(ref offset, sendRequestDone);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequest_ToServerAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeRequest_ToServerAsync({invokeRequest})", invokeRequest);

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
        var activeStreams = new ConcurrentDictionary<Guid, (IAsyncEnumerator<T> enumerator, SemaphoreSlim gate)>();
        ArgumentRequestHandlers[(requestId, argumentIndex)] = async (streamId, ct) =>
        {
            var (enumerator, gate) = activeStreams.GetOrAdd(streamId, _ => (source.GetAsyncEnumerator(cancellationToken), new SemaphoreSlim(1, 1)));
            await gate.WaitAsync(ct);
            try
            {
                var hasNext = await enumerator.MoveNextAsync();
                await Send_InvokeArgumentResponse_ToServerAsync(new InvokeArgumentResponseDto(
                    requestId,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    hasNext ? serializer(enumerator.Current) : []), ct);
                if (!hasNext)
                {
                    activeStreams.TryRemove(streamId, out _);
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
        return new RemoteAsyncEnumerable<T>((streamId, push, complete, ct) =>
        {
            var key = (requestId, argumentIndex, streamId);
            if (!ArgumentResponseHandlers.ContainsKey(key))
            {
                ArgumentResponseHandlers[key] = response =>
                {
                    if (response.IsCompleted)
                    {
                        ArgumentResponseHandlers.TryRemove(key, out _);
                        complete(null);
                    }
                    else
                    {
                        push(deserializer(response.BinaryData));
                    }
                };
            }
            return Send_InvokeArgumentRequest_ToServerAsync(new InvokeArgumentRequestDto(
                requestId,
                argumentIndex,
                streamId), ct);
        });
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

            int offset = 0;
            try
            {
                // 🚀 direct serializen in pooled buffer
                //var
                offset = item(span);
            }
            catch (Exception ex)
            {
            }

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