using gAPI.Core.Client.Config;
using gAPI.Core.Client.Helpers;
using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace gAPI.Core.Client.Wss;

public abstract class WssClientConnection : IWssClientConnection
{
    public WssClientConnection(
        ClientConfig config,
        IClientAuthenticatedHttpClient httpClient)
    {
        Config = config;
        HttpClient = httpClient;
        Sender = new WssClientConnectionSender(config, this);
        Logger = ((IClientLoggerFactory)this).CreateLogger<WssClientConnection>();
        ReceiveBuffer = new byte[config.MaxPackageSize ?? 8 * 1024 * 1024];
    }

    readonly WssClientConnectionSender Sender;
    readonly ILogger Logger;
    readonly SemaphoreSlim InitLock = new(1, 1);
    readonly ConcurrentDictionary<string, SubscribeDto> Subscriptions = [];
    readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingResponseClientDto>> StreamingResponseHandlers = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneClientDto>> PendingSendRequests = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneClientDto>> PendingInvokeRequests = [];
    readonly ConcurrentDictionary<RequestId, LinkedCancellationTokenSourceWithTimeout> Timeouts = [];
    readonly byte[] ReceiveBuffer;

    private Task? InitializeTask;
    private ClientWebSocket? Ws;

    public ClientConfig Config { get; }

    protected readonly IClientAuthenticatedHttpClient HttpClient;
    protected CancellationTokenSource? Cts;

    public bool Initialized { get; private set; }
    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricConnectionId FabricConnectionId { get; private set; } = new FabricConnectionId(-1);
    public ClientConnectionId ClientConnectionId { get; private set; } = new ClientConnectionId(-1);

    public bool IsConnected => Ws?.State == WebSocketState.Open;
    public SessionId SessionId => HttpClient.SessionId;

    public LogLevel MinimalLevel => Config.MinimumLogLevel;

    #region Connection

    public async Task TryConnectAsync(CancellationToken ct)
    {
        await InitLock.WaitAsync(ct);
        try
        {
            if (IsConnected)
                return;

            // Als we niet verbonden zijn, moeten we de oude tafelen/resetten
            if (InitializeTask == null || InitializeTask.IsFaulted || InitializeTask.IsCompleted)
            {
                InitializeTask = ConnectAsync(Config.WssBackendUrl!, ct);
            }
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

                // ❌ VERWIJDER OF COMMENTEER DEZE REGEL:
                // Ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(5); // <-- Dit veroorzaakte de crash op WASM

                var url = new Uri($"{baseUri}/fabricr?SessionId={sessionId}");
                await Ws.ConnectAsync(url, ct);

                _ = Task.Run(async () => { await ReceiverKernel(Ws, Cts); }, Cts.Token);
                _ = Task.Run(async () => { await Sender.SendKernel(Ws, Cts.Token); }, Cts.Token);

                var initialize = new InitializeDto()
                {
                    StateData = stateData,
                };
                await Sender.Send_Initialize_ToServerAsync(initialize, sessionId, Cts.Token);

                Logger.LogInformation("Verbinding hersteld, bezig met opnieuw inschrijven op {count} subscripties...", Subscriptions.Count);
                await Resubscribe(Cts.Token);

                Initialized = true;
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                await InitLock.WaitAsync(CancellationToken.None);
                try { InitializeTask = null; } finally { InitLock.Release(); }
                throw;
            }
            catch (Exception ex)
            {
                Initialized = false;
                Logger.LogWarning("ConnectAsync => connection failed, retrying: {ex}", ex.Message);

                await InitLock.WaitAsync(CancellationToken.None);
                try { InitializeTask = null; } finally { InitLock.Release(); }

                try
                {
                    Cts?.Cancel();
                    Ws?.Dispose();
                }
                catch { }

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

            // 🎯 NIEUW: Maak de verzendwachtrij direct leeg!
            // Oude, niet-verzonden berichten mogen NOOIT over de nieuwe socket gaan.
            Sender.ClearQueue();

            // Laat alle hangende verzoeken direct falen zodat de UI/aanroeper weet dat de verbinding weg is
            foreach (var pending in PendingSendRequests.Values)
                pending.TrySetException(new Exception("Verbinding verbroken vanwege netwerkwissel."));

            foreach (var pending in PendingInvokeRequests.Values)
                pending.TrySetException(new Exception("Verbinding verbroken vanwege netwerkwissel."));

            PendingSendRequests.Clear();
            PendingInvokeRequests.Clear();
            Timeouts.Clear();

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

        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now}: Send_Subscribe_ToServerAsync({subscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe);

        Subscriptions[subscribe.ToString()] = subscribe;

        await Sender.Send_Subscribe_ToServerAsync(subscribe, ct);
    }
    public async Task Send_Unsubscribe_ToServerAsync(UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (!Initialized)
            return;

        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now}: Send_Unsubscribe_ToServerAsync({unsubscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe);

        Subscriptions.Remove(unsubscribe.ToString(), out _);

        await Sender.Send_Unsubscribe_ToServerAsync(unsubscribe, ct);
    }
    public async Task Resubscribe(CancellationToken ct)
    {
        foreach (var subscribe in Subscriptions.Values)
        {
            await Sender.Send_Subscribe_ToServerAsync(subscribe, ct);
        }
    }

    private async Task Send_StreamingRequest_ToServerAsync(RoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now}: Send_SendRequest_ToApiAsync({routing}, {argumentIndex}, {streamId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing, argumentIndex, streamId);

        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var request = new StreamingRequestClientDto(
            routing,
            argumentIndex,
            streamId,
            cancelled,
            stateIsChanged,
            stateData);
        await Sender.Send_StreamingRequest_ToServerAsync(request, ct);
    }
    private async Task Send_FabricStreamingRequest_ToServerAsync(RoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now}: Send_FabricStreamingRequest_ToServerAsync({routing}, {argumentIndex}, {streamId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing, argumentIndex, streamId);

        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var request = new StreamingRequestClientDto(
            routing,
            argumentIndex,
            streamId,
            cancelled,
            stateIsChanged,
            stateData);
        await Sender.Send_FabricStreamingRequest_ToServerAsync(request, ct);
    }

    #endregion

    #region Receiver
    private async Task ReceiverKernel(WebSocket socket, CancellationTokenSource cts)
    {
        var ct = cts.Token;
        bool explicitClose = false;

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
                        explicitClose = true; // Netjes afgesloten door server of client
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

                    case WssServerToClientMessageEnum.FabricSendRequest:
                        var sendArgumentedRequest = span.ReadSendRequestClientDto(ref offset);
                        await Received_FabricSendRequest_FromServer(sendArgumentedRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricSendRequestCancelled:
                        var sendRequestCancelled = span.ReadSendRequestCancelledClientDto(ref offset);
                        await Received_FabricSendRequestCancelled_FromServer(sendRequestCancelled, ct);
                        break;
                    case WssServerToClientMessageEnum.SendRequestDone:
                        var sendArgumentedRequestDone = span.ReadSendRequestDoneClientDto(ref offset);
                        await Received_SendRequestDone_FromServer(sendArgumentedRequestDone, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricInvokeRequest:
                        var invokeRequest = span.ReadInvokeRequestClientDto(ref offset);
                        await Received_FabricInvokeRequest_FromServerAsync(invokeRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricInvokeRequestCancelled:
                        var invokeRequestCancelled = span.ReadInvokeRequestCancelledClientDto(ref offset);
                        await Received_FabricInvokeRequestCancelled_FromServerAsync(invokeRequestCancelled, ct);
                        break;
                    case WssServerToClientMessageEnum.InvokeRequestDone:
                        var invokeRequestDone = span.ReadInvokeRequestDoneClientDto(ref offset);
                        await Received_InvokeRequestDone_FromServerAsync(invokeRequestDone, ct);
                        break;

                    case WssServerToClientMessageEnum.StreamingRequest:
                        var argumentRequest = span.ReadStreamingRequestClientDto(ref offset);
                        await Received_StreamingRequest_FromServerAsync(argumentRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.StreamingResponse:
                        var argumentResponse = span.ReadStreamingResponseClientDto(ref offset);
                        await Received_StreamingResponse_FromServer(argumentResponse, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricStreamingRequest:
                        var fabricArgumentRequest = span.ReadStreamingRequestClientDto(ref offset);
                        await Received_FabricStreamingRequest_FromServerAsync(fabricArgumentRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricStreamingResponse:
                        var fabricArgumentResponse = span.ReadStreamingResponseClientDto(ref offset);
                        await Received_FabricStreamingResponse_FromServer(fabricArgumentResponse, ct);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("ReceiverKernel => Exception: {ex}", ex);
            throw;
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();

            // 🎯 ALS de verbinding onverwacht is verbroken (bv. door netwerkwissel), start reconnect flow:
            if (!explicitClose && !ct.IsCancellationRequested)
            {
                Logger.LogInformation("Verbinding onverwacht verloren. Herverbinden over 5 seconden...");
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        // ForceReconnectAsync reset de state netjes en start TryConnectAsync opnieuw
                        await ForceReconnectAsync(CancellationToken.None);
                    }
                    catch (Exception reconEx)
                    {
                        Logger.LogError("Automatische herverbinding mislukt: {ex}", reconEx);
                    }
                });
            }
        }
    }

    private async Task Received_SynchronizeClientIds_FromServer(SynchronizeClientIdsDto synchronizeClientIds, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SynchronizeClientIds_FromServer({synchronizeClientIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeClientIds);

            FabricManagerId = synchronizeClientIds.FabricManagerId;
            FabricConnectionId = synchronizeClientIds.FabricConnectionId;
            ClientConnectionId = synchronizeClientIds.ClientConnectionId;
        }, ct);
    }

    private async Task Received_FabricSendRequest_FromServer(SendRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SendRequest_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(100), ct);
            Timeouts[message.Routing.RequestId] = cts;

            try
            {
                if (message.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(message.StateData, cts.Token);

                await Send_SendRequest_ToServiceAsync(message, cts.Token);

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_SendRequestDone_ToServerAsync(
                    new SendRequestDoneClientDto(
                        message.Routing,
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
                        message.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData
                    ), ct);

                cts.Dispose();
                Timeouts.TryRemove(message.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Received_FabricSendRequestCancelled_FromServer(SendRequestCancelledClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SendRequest_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (Timeouts.TryRemove(message.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            await Sender.Send_SendRequestDone_ToServerAsync(
                new SendRequestDoneClientDto(
                    message.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Received_SendRequestDone_FromServer(SendRequestDoneClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SendRequestDone_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (PendingSendRequests.TryRemove(message.Routing.RequestId, out var completion))
                completion.TrySetResult(message);
        }, ct);
    }

    private async Task Received_FabricInvokeRequest_FromServerAsync(InvokeRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SendRequestDone_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var cts = new LinkedCancellationTokenSourceWithTimeout(
                TimeSpan.FromSeconds(100),
                ct);
            Timeouts[message.Routing.RequestId] = cts;



            try
            {
                if (message.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(message.StateData, cts.Token);

                var enumerable = Send_InvokeRequest_ToServiceAsync(message, cts.Token);
                RegisterAsyncEnumerableArgumentByte(message.Routing, -1, enumerable, cts.Token);

                cts.OnDispose = async () =>
                {
                    await UnRegisterAsyncEnumerableArgument(message.Routing, -1);
                };

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_InvokeRequestDone_ToServerAsync(
                    new InvokeRequestDoneClientDto(
                        message.Routing,
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
                        message.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData),
                    ct);

                cts.Dispose();
                Timeouts.TryRemove(message.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Received_FabricInvokeRequestCancelled_FromServerAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_InvokeRequestCancelled_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (Timeouts.TryRemove(message.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            await Sender.Send_InvokeRequestDone_ToServerAsync(
                new InvokeRequestDoneClientDto(
                    message.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Received_InvokeRequestDone_FromServerAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_InvokeRequestDone_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (PendingInvokeRequests.TryRemove(message.Routing.RequestId, out var ___channel))
                ___channel.TrySetResult(message);
        }, ct);
    }

    private async Task Received_StreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_StreamingRequest_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(message.StreamId, message.Cancelled, ct);
                await Sender.Send_StreamingResponse_ToServerAsync(response, ct);

                //StreamingResponseHandlers
                //PendingStreamingResponses.TryRemove((routing.RequestId, argumentIndex, streamId), out response!)

                //if (FabricClient.TryTakeStreamingResponse(
                //    streamingRequest.Routing,
                //    streamingRequest.ArgumentIndex,
                //    streamingRequest.StreamId,
                //    out var response))
                //{
                //    // Terug naar CLIENT!!! (dat is er anders)
                //    await Send_StreamingResponse_ToClientAsync(response, ct);
                //}
            }
        }, ct);
    }
    private async Task Received_StreamingResponse_FromServer(StreamingResponseClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_StreamingResponse_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
                responseHandler(message);
        }, ct);
    }

    private async Task Received_FabricStreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_FabricStreamingRequest_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(message.StreamId, message.Cancelled, ct);
                await Sender.Send_FabricStreamingResponse_ToServerAsync(response, ct);
            }
        }, ct);
    }
    private async Task Received_FabricStreamingResponse_FromServer(StreamingResponseClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_FabricStreamingResponse_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
                responseHandler(message);
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
    public async Task Send_FireAndForget_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        try
        {
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var sendRequest = new SendRequestClientDto(routing, data, stateIsChanged, stateData);

            // Gebruik je eigen token voor de queueing
            await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("FireAndForget chunk afgebroken vanwege time-out (30s).");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fout opgevangen in FireAndForget streaming bus-call");
        }
    }
    public async Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var sendRequest = new SendRequestClientDto(routing, data, stateIsChanged, stateData);

        var completion = PendingSendRequests.GetOrAdd(
            routing.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously));

        await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

        try
        {
            ct.Register(async () => {
                var requestCancelled = new SendRequestCancelledClientDto(routing, "Task is cancelled", false, null);
                await Sender.Send_SendRequestCancelled_ToServerAsync(requestCancelled, ct);
                PendingSendRequests.TryRemove(routing.RequestId, out _);
            });

            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
        var invokeRequest = new InvokeRequestClientDto(routing, data, stateIsChanged, stateData);

        var completion = PendingInvokeRequests.GetOrAdd(
            routing.RequestId,
            _ => new TaskCompletionSource<InvokeRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously));

        await Sender.Send_InvokeRequest_ToServerAsync(invokeRequest, ct);

        try
        {
            ct.Register(async () => {
                var invokeCancelled = new InvokeRequestCancelledClientDto(routing, "Task is cancelled", false, null);
                await Sender.Send_InvokeRequestCancelled_ToServerAsync(invokeCancelled, ct);
                PendingSendRequests.TryRemove(routing.RequestId, out _);
            });

            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({serviceId}, {methodId}, {requestId}, {argumentIndex})",
                routing.ServiceId, routing.MethodId, routing.RequestId,
                argumentIndex);

        return new RemoteAsyncEnumerable<byte[]>(
            construct: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, response =>
                {
                    if (response.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(response.ExceptionMessage));
                        return;
                    }

                    if (response.ExceptionMessage != null)
                    {
                        enumerator.Complete(new RemoteException(response.ExceptionMessage));
                        return;
                    }

                    if (response.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    enumerator.Push(response.BinaryData);
                });
            },

            requestNext: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator, CancellationToken ct) =>
            {
                return Send_StreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    false,
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator) =>
            {
                return Send_StreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    default);
                //return Send_StreamingCancelled_ToServerAsync(
                //    routing,
                //    argumentIndex,
                //    streamId);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryRemove(key, out _);
            });
    }
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterRemoteAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        return new RemoteAsyncEnumerable<T>(
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, response =>
                {
                    if (response.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(response.ExceptionMessage));
                        return;
                    }

                    if (response.ExceptionMessage != null)
                    {
                        enumerator.Complete(new RemoteException(response.ExceptionMessage));
                        return;
                    }

                    if (response.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    try
                    {
                        enumerator.Push(deserializer.Invoke(response.BinaryData));
                    }
                    catch (Exception ex)
                    {
                        enumerator.Complete(ex);
                    }
                });
            },

            requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                return Send_FabricStreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    false,
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
            {
                return Send_FabricStreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    default);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryRemove(key, out _);
            });
    }

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        StreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new AsyncEnumerableRegistration<T>(async (
            ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return new AsyncEnumerableRegistrationInstance<T>(
                        source.GetAsyncEnumerator(linked.Token),
                        new SemaphoreSlim(1, 1),
                        linked);
                });

            async Task Cleanup()
            {
                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
            }

            var entered = false;
            try
            {
                await gate.WaitAsync(ct);
                entered = true;

                var hasNext = !cancelled && await enumerator.MoveNextAsync();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                var response = new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    cancelled,
                    null,
                    hasNext ? serializer(enumerator.Current) : [],
                    stateIsChanged,
                    stateData);

                if (!hasNext)
                    await Cleanup();

                return response;
            }
            catch (OperationCanceledException ex) when (
                ct.IsCancellationRequested ||
                cancellationToken.IsCancellationRequested)
            {
                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    ex.Message,
                    [],
                    stateIsChanged,
                    stateData);
            }
            catch (Exception ex)
            {
                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    ex.Message,
                    [],
                    stateIsChanged,
                    stateData);
            }
            finally
            {
                if (entered)
                    gate.Release();
            }
        }));
    }
    public void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        StreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new AsyncEnumerableRegistration<byte[]>(async (
            ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<byte[]>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return new AsyncEnumerableRegistrationInstance<byte[]>(
                        source.GetAsyncEnumerator(linked.Token),
                        new SemaphoreSlim(1, 1),
                        linked);
                });

            async Task Cleanup()
            {
                activeStreams.TryRemove(streamId, out _);
                await enumerator.DisposeAsync();
                linkedCts.Dispose();
            }

            var entered = false;
            try
            {
                await gate.WaitAsync(ct);
                entered = true;

                var hasNext = !cancelled && await enumerator.MoveNextAsync();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                var response = new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    cancelled,
                    null,
                    hasNext ? enumerator.Current : [],
                    stateIsChanged,
                    stateData);

                if (!hasNext)
                    await Cleanup();

                return response;
            }
            catch (OperationCanceledException ex) when (
                ct.IsCancellationRequested ||
                cancellationToken.IsCancellationRequested)
            {
                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    ex.Message,
                    [],
                    stateIsChanged,
                    stateData);
            }
            catch (Exception ex)
            {
                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    ex.Message,
                    [],
                    stateIsChanged,
                    stateData);
            }
            finally
            {
                if (entered)
                    gate.Release();
            }
        }));
    }

    public async Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} UnRegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        if (StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
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
        Cts?.Cancel();
        Cts?.Dispose();
        Cts = null;
        Ws?.Dispose();
        Ws = null;
        GC.SuppressFinalize(this);
    }
}