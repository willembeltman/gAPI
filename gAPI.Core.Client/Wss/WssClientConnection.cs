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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Xml.Linq;

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
        Sender = new WssClientConnectionSender(this);
        Logger = ((IClientLoggerFactory)this).CreateLogger<WssClientConnection>();
    }

    readonly WssClientConnectionSender Sender;
    readonly string WssBackendUrl;
    readonly ILogger Logger;
    readonly SemaphoreSlim InitLock = new(1, 1);
    readonly ConcurrentDictionary<string, SubscribeDto> Subscriptions = [];
    readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Func<StreamId, CancellationToken, Task>> StreamingRequestHandlers = [];
    readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex, StreamId StreamId), Action<StreamingResponseClientDto>> StreamingResponseHandlers = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneClientDto>> PendingSendRequests = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneClientDto>> PendingInvokeRequests = [];
    //readonly ConcurrentDictionary<RequestId, ResettableTimeout> Timeouts = [];
    readonly ConcurrentDictionary<RequestId, LinkedCancellationTokenSourceWithTimeout> Timeouts = [];
    readonly byte[] ReceiveBuffer = new byte[10 * 1024 * 1024];

    private Task? InitializeTask;
    private ClientWebSocket? Ws;

    protected readonly IClientAuthenticatedHttpClient HttpClient;
    protected CancellationTokenSource? Cts;

    public bool Initialized { get; private set; }
    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricConnectionId FabricConnectionId { get; private set; } = new FabricConnectionId(-1);
    public ClientConnectionId ClientConnectionId { get; private set; } = new ClientConnectionId(-1);

    public bool IsConnected => Ws?.State == WebSocketState.Open;
    public SessionId SessionId => HttpClient.SessionId;

    #region Connection

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
                _ = Task.Run(async () => { await Sender.SendKernel(Ws, Cts.Token); }, Cts.Token);

                var initialize = new InitializeDto()
                {
                    StateData = stateData,
                };
                await Sender.Send_Initialize_ToServerAsync(initialize, Cts.Token);

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
    private void HttpClient_OnStateHasChanged()
    {
        if (HttpClient.ForceReconnect)
        {
            HttpClient.ForceReconnect = false;
            _ = ForceReconnectAsync(new());
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

    #endregion

    #region Sender

    public async Task Send_Subscribe_ToServerAsync(SubscribeDto subscribe, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Subscribe_ToServerAsync({subscribe})", subscribe);

        Subscriptions[subscribe.ToString()] = subscribe;

        await Sender.Send_Subscribe_ToServerAsync(subscribe, ct);
    }
    public async Task Send_Unsubscribe_ToServerAsync(UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (!Initialized)
            return;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Unsubscribe_ToServerAsync({unsubscribe})", unsubscribe);

        Subscriptions.Remove(unsubscribe.ToString(), out _);

        await Sender.Send_Unsubscribe_ToServerAsync(unsubscribe, ct);
    }

    private async Task Send_StreamingRequest_ToServerAsync(RoutingDto routing, int argumentIndex, StreamId streamId, CancellationToken ct)
    {
        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var request = new StreamingRequestClientDto(
            routing,
            argumentIndex,
            streamId,
            stateIsChanged,
            stateData);
        await Sender.Send_StreamingRequest_ToServerAsync(request, ct);
    }

    #endregion

    #region Receiver
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
                    case WssServerToClientMessageEnum.SynchronizeClientIds:
                        var synchronizeClientIds = span.ReadSynchronizeClientIdsDto(ref offset);
                        await Received_SynchronizeClientIds_FromServer(synchronizeClientIds, ct);
                        break;


                    case WssServerToClientMessageEnum.SendRequest:
                        var sendArgumentedRequest = span.ReadSendRequestClientDto(ref offset);
                        await Received_SendRequest_FromServer(sendArgumentedRequest, ct);
                        break;

                    case WssServerToClientMessageEnum.SendRequestDone:
                        var sendArgumentedRequestDone = span.ReadSendRequestDoneClientDto(ref offset);
                        await Received_SendRequestDone_FromServer(sendArgumentedRequestDone, ct);
                        break;

                    case WssServerToClientMessageEnum.SendRequestCancelled:
                        var sendRequestCancelled = span.ReadSendRequestCancelledClientDto(ref offset);
                        await Received_SendRequestCancelled_FromServer(sendRequestCancelled, ct);
                        break;


                    case WssServerToClientMessageEnum.InvokeRequest:
                        var invokeRequest = span.ReadInvokeRequestClientDto(ref offset);
                        await Received_InvokeRequest_FromServerAsync(invokeRequest, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeCancelled:
                        var invokeRequestCancelled = span.ReadInvokeRequestCancelledClientDto(ref offset);
                        await Received_InvokeCancelled_FromServer(invokeRequestCancelled, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeRequestDone:
                        var invokeResponseDone = span.ReadInvokeRequestDoneClientDto(ref offset);
                        await Received_InvokeRequestDone_FromServerAsync(invokeResponseDone, ct);
                        break;


                    case WssServerToClientMessageEnum.StreamingRequest:
                        var argumentRequest = span.ReadStreamingRequestClientDto(ref offset);
                        await Received_StreamingRequest_FromServerAsync(argumentRequest, ct);
                        break;

                    case WssServerToClientMessageEnum.StreamingResponse:
                        var argumentResponse = span.ReadStreamingResponseClientDto(ref offset);
                        await Received_StreamingResponse_FromServer(argumentResponse, ct);
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

    private async Task Received_SynchronizeClientIds_FromServer(SynchronizeClientIdsDto synchronizeClientIds, CancellationToken ct)
    {
        FabricManagerId = synchronizeClientIds.FabricManagerId;
        FabricConnectionId = synchronizeClientIds.FabricConnectionId;
        ClientConnectionId = synchronizeClientIds.ClientConnectionId;
    }

    private async Task Received_SendRequest_FromServer(SendRequestClientDto sendRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(30), ct);
            Timeouts[sendRequest.Routing.RequestId] = cts;

            try
            {
                if (sendRequest.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(sendRequest.StateData, cts.Token);

                await Send_SendRequest_ToServiceAsync(sendRequest, cts.Token);

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_SendRequestDone_ToServerAsync(
                    new SendRequestDoneClientDto(
                        sendRequest.Routing,
                        false,
                        null,
                        stateIsChanged,
                        stateData
                    ), ct);
            }
            catch (Exception ex)
            {
                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_SendRequestDone_ToServerAsync(
                    new SendRequestDoneClientDto(
                        sendRequest.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData
                    ), ct);

                cts.Dispose();
                Timeouts.TryRemove(sendRequest.Routing.RequestId, out _);
            }
        }, ct);
    }
    //sendRequestCancelled
    private async Task Received_SendRequestCancelled_FromServer(SendRequestCancelledClientDto sendRequestCancelled, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (sendRequestCancelled.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(sendRequestCancelled.StateData, ct);

            if (Timeouts.TryRemove(sendRequestCancelled.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            await Sender.Send_SendRequestDone_ToServerAsync(
                new SendRequestDoneClientDto(
                    sendRequestCancelled.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Received_SendRequestDone_FromServer(SendRequestDoneClientDto sendRequestDone, CancellationToken ct)
    {
        if (PendingSendRequests.TryRemove(sendRequestDone.Routing.RequestId, out var completion))
            completion.TrySetResult(sendRequestDone);
    }

    private async Task Received_InvokeRequest_FromServerAsync(InvokeRequestClientDto invokeRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(30), ct);
            Timeouts[invokeRequest.Routing.RequestId] = cts;

            try
            {
                if (invokeRequest.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(invokeRequest.StateData, cts.Token);

                var source = Send_InvokeRequest_ToServiceAsync(invokeRequest, cts.Token);
                RegisterAsyncEnumerableArgumentByte(invokeRequest.Routing, -1, source, cts.Token);

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_InvokeRequestDone_ToServerAsync(
                    new InvokeRequestDoneClientDto(
                        invokeRequest.Routing,
                        false,
                        null,
                        stateIsChanged,
                        stateData
                    ), ct);
            }
            catch (Exception ex)
            {
                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_SendRequestDone_ToServerAsync(
                    new SendRequestDoneClientDto(
                        invokeRequest.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData),
                    ct);

                cts.Dispose();
                Timeouts.TryRemove(invokeRequest.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Received_InvokeCancelled_FromServer(InvokeRequestCancelledClientDto invokeRequestCancelled, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_InvokeCancelled_FromClientAsync({invokeRequestCancelled})", invokeRequestCancelled);

            if (invokeRequestCancelled.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(invokeRequestCancelled.StateData, ct);

            if (Timeouts.TryRemove(invokeRequestCancelled.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            await Sender.Send_InvokeRequestDone_ToServerAsync(
                new InvokeRequestDoneClientDto(
                    invokeRequestCancelled.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Received_InvokeRequestDone_FromServerAsync(InvokeRequestDoneClientDto invokeResponseDone, CancellationToken ct)
    {
        if (PendingInvokeRequests.TryRemove(invokeResponseDone.Routing.RequestId, out var ___channel))
            ___channel.TrySetResult(invokeResponseDone);
    }

    private async Task Received_StreamingResponse_FromServer(StreamingResponseClientDto argumentResponse, CancellationToken ct)
    {
        if (Timeouts.TryGetValue(argumentResponse.Routing.RequestId, out var timeout))
            timeout.Reset();

        if (StreamingResponseHandlers.TryGetValue((argumentResponse.Routing.RequestId, argumentResponse.ArgumentIndex, argumentResponse.StreamId), out var responseHandler))
            responseHandler(argumentResponse);
    }
    private async Task Received_StreamingRequest_FromServerAsync(StreamingRequestClientDto argumentRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Timeouts.TryGetValue(argumentRequest.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingRequestHandlers.TryGetValue((argumentRequest.Routing.RequestId, argumentRequest.ArgumentIndex), out var argumentHandler))
                await argumentHandler(argumentRequest.StreamId, ct);
        }, ct);
    }

    #endregion

    #region Calls naar de service

    protected abstract Task Send_SendRequest_ToServiceAsync(
        SendRequestDto sendRequest,
        CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(
        InvokeRequestDto invokeRequest,
        CancellationToken ct);

    #endregion

    #region Call's vanuit gegenereerde code

    public async Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        if (!Initialized)
            return;

        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var sendRequest = new SendRequestClientDto(routing, stateIsChanged, stateData, data);

        var completion = PendingSendRequests.GetOrAdd(
            routing.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously));

        await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    ct);
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);
        }
        finally
        {
            PendingSendRequests.TryRemove(routing.RequestId, out _);
        }
    }
    public async IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, [EnumeratorCancellation] CancellationToken ct)
    {
        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var invokeRequest = new InvokeRequestClientDto(routing, stateIsChanged, stateData, data);

        var completion = PendingInvokeRequests.GetOrAdd(
            routing.RequestId,
            _ => new TaskCompletionSource<InvokeRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously));

        await Sender.Send_InvokeRequest_ToServerAsync(invokeRequest, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    ct);
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);

            var list = RegisterRemoteAsyncEnumerableArgumentByte(routing, -1);
            await foreach (var item in list)
            {
                yield return item;
            }
        }
        finally
        {
            PendingSendRequests.TryRemove(routing.RequestId, out _);
        }
    }

    protected IAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        return new RemoteAsyncEnumerable<byte[]>((streamId, push, complete, ct) =>
        {
            var key = (routing.RequestId, argumentIndex, streamId);
            if (!StreamingResponseHandlers.ContainsKey(key))
            {
                StreamingResponseHandlers[key] = response =>
                {
                    if (response.IsCompleted)
                    {
                        StreamingResponseHandlers.TryRemove(key, out _);
                        complete(null);
                    }
                    else
                    {
                        push(response.BinaryData);
                    }
                };
            }
            return Send_StreamingRequest_ToServerAsync(routing, argumentIndex, streamId, ct);
        });
    }
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>((streamId, push, complete, ct) =>
        {
            var key = (routing.RequestId, argumentIndex, streamId);
            if (!StreamingResponseHandlers.ContainsKey(key))
            {
                StreamingResponseHandlers[key] = response =>
                {
                    if (response.IsCompleted)
                    {
                        StreamingResponseHandlers.TryRemove(key, out _);
                        complete(null);
                    }
                    else
                    {
                        push(deserializer(response.BinaryData));
                    }
                };
            }
            return Send_StreamingRequest_ToServerAsync(routing, argumentIndex, streamId, ct);
        });
    }
    protected void UnRegisterRemoteAsyncEnumerableArguments(RoutingDto requestId)
    {

    }

    public void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken)
    {
        var activeStreams = new ConcurrentDictionary<StreamId, (IAsyncEnumerator<byte[]> enumerator, SemaphoreSlim gate, CancellationTokenSource linkedCts)>();
        StreamingRequestHandlers[(routing.RequestId, argumentIndex)] = async (streamId, ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return (source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
                });

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

            await gate.WaitAsync(ct);
            try
            {
                var hasNext = await enumerator.MoveNextAsync();
                await Sender.Send_StreamingResponse_ToServerAsync(new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    stateIsChanged,
                    stateData,
                    hasNext ? enumerator.Current : []), ct);
                if (!hasNext)
                {
                    activeStreams.TryRemove(streamId, out _);
                    await enumerator.DisposeAsync();
                    linkedCts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    throw;

                await Sender.Send_StreamingResponse_ToServerAsync(new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    stateIsChanged,
                    stateData,
                    []), CancellationToken.None);

                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
            }
            finally
            {
                gate.Release();
            }
        };
    }
    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        var activeStreams = new ConcurrentDictionary<StreamId, (IAsyncEnumerator<T> enumerator, SemaphoreSlim gate, CancellationTokenSource linkedCts)>();
        StreamingRequestHandlers[(routing.RequestId, argumentIndex)] = async (streamId, ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return (source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
                });

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

            await gate.WaitAsync(ct);
            try
            {
                var hasNext = await enumerator.MoveNextAsync();
                await Sender.Send_StreamingResponse_ToServerAsync(new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    stateIsChanged,
                    stateData,
                    hasNext ? serializer(enumerator.Current) : []), ct);
                if (!hasNext)
                {
                    activeStreams.TryRemove(streamId, out _);
                    await enumerator.DisposeAsync();
                    linkedCts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    throw;

                await Sender.Send_StreamingResponse_ToServerAsync(new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    stateIsChanged,
                    stateData,
                    []), CancellationToken.None);

                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
            }
            finally
            {
                gate.Release();
            }
        };
    }
    public void UnRegisterAsyncEnumerableArguments(RoutingDto routing)
    {

    }

    #endregion 

    #region ILoggerProvider

    public ILogger CreateLogger(string categoryName)
        => new ClientLoggerFactory(categoryName, this);
    public void AddProvider(ILoggerProvider provider)
    {
        // no-op
    }
    public Task Send_Log_ToServerAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        return Sender.Send_Log_ToServerAsync(log, ct);
    }

    #endregion

    public void Dispose()
    {
        HttpClient.OnStateHasChanged -= HttpClient_OnStateHasChanged;
        Cts?.Cancel();
        Cts?.Dispose();
        Ws?.Dispose();
        GC.SuppressFinalize(this);
    }
}