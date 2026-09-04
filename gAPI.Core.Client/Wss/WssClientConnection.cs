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

    private readonly WssClientConnectionSender Sender;
    private readonly string WssBackendUrl;
    private readonly ILogger Logger;
    private readonly SemaphoreSlim InitLock = new(1, 1);
    private readonly ConcurrentDictionary<string, SubscribeDto> Subscriptions = [];
    private readonly ConcurrentDictionary<(RoutingDto RequestId, int ArgumentIndex), Func<StreamId, CancellationToken, Task>> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<(RoutingDto RequestId, int ArgumentIndex, StreamId StreamId), Action<StreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RoutingDto, TaskCompletionSource<SendRequestDoneDto>> PendingRequests = [];
    private readonly ConcurrentDictionary<RoutingDto, ResettableTimeout> Timeouts = [];
    private readonly byte[] ReceiveBuffer = new byte[10 * 1024 * 1024];

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

    #region ToService

    protected abstract Task Send_SendRequest_ToServiceAsync(
        SendRequestDto sendRequest,
        CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(
        InvokeRequestDto invokeRequest,
        CancellationToken ct);

    #endregion

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

    #region Generated endpoints / Remote enumerable

    #endregion

    //#region Generated endpoints / Invoke request channels

    //public void RegisterInvokeRequest(RequestId requestId, Channel<InvokeResponseDto> channel)
    //{
    //    PendingInvokeRequests[requestId] = channel;
    //    Timeouts[requestId] = new ResettableTimeout(TimeSpan.FromSeconds(60), () =>
    //    {
    //        if (PendingInvokeRequests.TryRemove(requestId, out var pending))
    //            pending.Writer.TryComplete(new TimeoutException("Invoke request timed out."));
    //        if (Timeouts.TryRemove(requestId, out var timeout))
    //            timeout.Dispose();
    //    });
    //}
    //public void UnregisterInvokeRequest(RequestId requestId)
    //{
    //    PendingInvokeRequests.TryRemove(requestId, out _);
    //    if (Timeouts.TryRemove(requestId, out var timeout))
    //        timeout.Dispose();
    //}

    //#endregion

    #region Sender (Call's vanuit gegenereerde code)

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

    public async Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        if (!Initialized)
            return;

        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var sendRequest = new SendRequestDto(routing, stateIsChanged, stateData, data);

        var completion = PendingRequests.GetOrAdd(
            sendRequest.Routing,
            _ => new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously));

        await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    ct);
        }
        finally
        {
            PendingRequests.TryRemove(sendRequest.Routing, out _);
        }
    }
    public async IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, [EnumeratorCancellation] CancellationToken ct)
    {
        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        InvokeRequestDto invokeRequest = new InvokeRequestDto(routing, stateIsChanged, stateData, data);

        yield return [];
        throw new NotImplementedException();

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeRequest_ToServerAsync({invokeRequest})", invokeRequest);

        var list = Sender.Send_InvokeRequest_ToServerAsync(invokeRequest, ct);
    }

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        var activeStreams = new ConcurrentDictionary<StreamId, (IAsyncEnumerator<T> enumerator, SemaphoreSlim gate, CancellationTokenSource linkedCts)>();
        StreamingRequestHandlers[(routing, argumentIndex)] = async (streamId, ct) =>
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
                await Sender.Send_StreamingResponse_ToServerAsync(new StreamingResponseDto(
                    HttpClient.SessionId,
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

                await Sender.Send_StreamingResponse_ToServerAsync(new StreamingResponseDto(
                    HttpClient.SessionId,
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

    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RoutingDto requestId, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>((streamId, push, complete, ct) =>
        {
            var key = (requestId, argumentIndex, streamId);
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
            return Sender.Send_StreamingRequest_ToServerAsync(new StreamingRequestDto(
                requestId,
                argumentIndex,
                streamId), ct);
        });
    }
    protected void UnRegisterRemoteAsyncEnumerableArgument(RoutingDto requestId)
    {

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

                    case WssServerToClientMessageEnum.SendRequestCancelled:
                        var sendRequestCancelled = span.ReadSendRequestCancelledDto(ref offset);
                        await Received_SendRequestCancelled_FromServer(sendRequestCancelled, ct);
                        break;

                    case WssServerToClientMessageEnum.StreamingRequest:
                        var argumentRequest = span.ReadStreamingRequestDto(ref offset);
                        _ = Task.Run(async () => { await Received_StreamingRequest_FromServerAsync(argumentRequest, ct); }, ct);
                        break;

                    case WssServerToClientMessageEnum.StreamingResponse:
                        var argumentResponse = span.ReadStreamingResponseDto(ref offset);
                        await Received_StreamingResponse_FromServer(argumentResponse, ct);
                        break;

                    case WssServerToClientMessageEnum.InvokeCancelled:
                        var invokeRequestCancelled = span.ReadInvokeRequestCancelledDto(ref offset);
                        await Received_InvokeCancelled_FromServer(invokeRequestCancelled, ct);
                        break;

                    //case WssServerToClientMessageEnum.InvokeResponse:
                    //    var invokeResponse = span.ReadInvokeResponseDto(ref offset);
                    //    await Received_InvokeResponse_FromServerAsync(invokeResponse, ct);
                    //    break;

                    case WssServerToClientMessageEnum.InvokeRequestDone:
                        var invokeResponseDone = span.ReadInvokeRequestDoneDto(ref offset);
                        await Received_InvokeRequestDone_FromServerAsync(invokeResponseDone, ct);
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

    private async Task Received_SendRequest_FromServer(SendRequestDto sendArgumentedRequest, CancellationToken ct)
    {
        try
        {
            if (sendArgumentedRequest.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(sendArgumentedRequest.StateData, ct);
            await Send_SendRequest_ToServiceAsync(sendArgumentedRequest, ct);
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            await Sender.Send_SendRequestDone_ToServerAsync(
                new SendRequestDoneDto(
                    sendArgumentedRequest.Routing,
                    stateIsChanged,
                    stateData,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            await Sender.Send_SendRequestDone_ToServerAsync(
                new SendRequestDoneDto(
                    sendArgumentedRequest.Routing,
                    stateIsChanged,
                    stateData,
                    ex.Message),
                ct);
        }
    }
    private async Task Received_SendRequestDone_FromServer(SendRequestDoneDto sendArgumentedRequestDone, CancellationToken ct)
    {
        if (PendingRequests.TryRemove(sendArgumentedRequestDone.Routing, out var completion))
            completion.TrySetResult(sendArgumentedRequestDone);
    }
    private async Task Received_SendRequestCancelled_FromServer(SendRequestCancelledDto sendRequestCancelled, CancellationToken ct)
    {
        PendingRequests.TryRemove(sendRequestCancelled.Routing, out _);
    }

    private async Task Received_InvokeRequest_FromServerAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
    {
        if (invokeRequest.StateIsChanged)
            await HttpClient.UpdateStateDataAsync(invokeRequest.StateData, ct);
        //RegisterAsyncEnumerableArgument(invokeRequest.Routing, -1, )
        var responses = Send_InvokeRequest_ToServiceAsync(invokeRequest, ct);


        // Todo: Enumerator registreren en streamid terug gevven
        throw new NotImplementedException();
        //await Send_InvokeRequestDone_ToServerAsync(
        //    new InvokeRequestDoneDto(
        //        invokeRequest.RequestId,
        //        [ streamId ]
        //    ), ct);
    }
    private async Task Received_InvokeCancelled_FromServer(InvokeRequestCancelledDto invokeRequestCancelled, CancellationToken ct)
    {
        //UnregisterInvokeRequest(invokeRequestCancelled.RequestId);
    }
    private async Task Received_InvokeRequestDone_FromServerAsync(InvokeRequestDoneDto invokeResponseDone, CancellationToken ct)
    {
        //if (PendingInvokeRequests.TryRemove(invokeResponseDone.RequestId, out var ___channel))
        //    ___channel.Writer.TryComplete();

        //UnregisterInvokeRequest(invokeResponseDone.RequestId);
    }

    private async Task Received_StreamingResponse_FromServer(StreamingResponseDto argumentResponse, CancellationToken ct)
    {
        if (Timeouts.TryGetValue(argumentResponse.Routing, out var timeout))
            timeout.Reset();

        if (StreamingResponseHandlers.TryGetValue((argumentResponse.Routing, argumentResponse.ArgumentIndex, argumentResponse.StreamId), out var responseHandler))
            responseHandler(argumentResponse);
    }
    private async Task Received_StreamingRequest_FromServerAsync(StreamingRequestDto argumentRequest, CancellationToken ct)
    {
        if (Timeouts.TryGetValue(argumentRequest.Routing, out var timeout))
            timeout.Reset();

        if (StreamingRequestHandlers.TryGetValue((argumentRequest.Routing, argumentRequest.ArgumentIndex), out var argumentHandler))
            await argumentHandler(argumentRequest.StreamId, ct);
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