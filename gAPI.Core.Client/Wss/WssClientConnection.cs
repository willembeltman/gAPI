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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
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
        var loggerFactory = (IClientLoggerFactory)this;
        Sender = new WssClientConnectionSender(this, config, loggerFactory);
        Logger = loggerFactory.CreateLogger<WssClientConnection>();
        ReceiveBuffer = new byte[config.MaxPackageSize ?? 512 * 1024];
    }

    readonly WssClientConnectionSender Sender;
    readonly ILogger Logger;
    readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingResponseClientDto>> StreamingResponseHandlers = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneClientDto>> PendingSendRequests = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestAckClientDto>> PendingInvokeRequests = [];
    readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> Timeouts = [];
    readonly byte[] ReceiveBuffer;

    private ClientWebSocket? Ws;

    public ClientConfig Config { get; }

    protected readonly IClientAuthenticatedHttpClient HttpClient;
    private CancellationToken? LifeCycleCt;

    public bool Initialized => ConnectedCts != null;
    public FabricManagerId FabricManagerId { get; private set; } = new FabricManagerId("Local");
    public FabricConnectionId FabricConnectionId { get; private set; } = new FabricConnectionId(-1);
    public ClientConnectionId ClientConnectionId { get; private set; } = new ClientConnectionId(-1);

    public bool IsConnected => Ws?.State == WebSocketState.Open;
    public SessionId SessionId => HttpClient.SessionId;

    public LogLevel MinimalLevel => Config.MinimumLogLevel;

    protected CancellationTokenSource? ConnectedCts;
    public bool RunningRequested { get; private set; }
    SemaphoreSlim ConnectingGate = new SemaphoreSlim(1);

    #region Connection

    public void Start(CancellationToken ct)
    {
        LifeCycleCt = ct;
        _ = Task.Run(ConnectAsync);
    }

    private async Task ConnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} ConnectAsync()", DateTime.Now.ToString("HH:mm:ss.fff"));

        await ConnectingGate.WaitAsync();
        try
        {
            RunningRequested = true;
            await Gated_ConnectAsync();
        }
        finally
        {
            ConnectingGate.Release();
        }
    }
    private async Task Gated_ConnectAsync()
    {
        var baseUri = Config.WssBackendUrl;
        while (RunningRequested == true && ConnectedCts == null)
        {
            try
            {
                var stateData = await HttpClient.GetStateDataAsync(false, LifeCycleCt ?? CancellationToken.None);
                var sessionId = HttpClient.SessionId.Value;

                Ws = new ClientWebSocket();

                var url = new Uri($"{baseUri}/fabricr?SessionId={sessionId}");
                await Ws.ConnectAsync(url, LifeCycleCt ?? CancellationToken.None);

                ConnectedCts = new CancellationTokenSource();
                var socket = Ws;
                var connectedCts = ConnectedCts;

                var initialize = new InitializeDto()
                {
                    StateData = stateData,
                    SessionId = sessionId
                };

                var senderTask = Sender.SendKernel(socket, initialize, connectedCts.Token);
                var receiverTask = ReceiverKernel(socket, connectedCts.Token);

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} ConnectAsync() Connected!", DateTime.Now.ToString("HH:mm:ss.fff"));

                _ = Task.Run(async () =>
                {
                    await Task.WhenAny(senderTask, receiverTask);
                    connectedCts.Cancel();
                    await Task.WhenAll(senderTask, receiverTask);

                    if (RunningRequested)
                        await ReconnectAsync();
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning("ConnectAsync => connection failed, retrying: {ex}", ex.Message);

                try
                {
                    ConnectedCts?.Cancel();
                    ConnectedCts?.Dispose();
                    ConnectedCts = null;
                    Ws?.Dispose();
                }
                catch { }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    private async Task ReconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError("{now} Reconnecting FabricClient ....",
                DateTime.Now.ToString("HH:mm:ss.fff"));

        await ConnectingGate.WaitAsync();
        try
        {
            await Gated_ReconnectAsync();
        }
        finally
        {
            ConnectingGate.Release();
        }
    }
    private async Task Gated_ReconnectAsync()
    {
        if (LifeCycleCt == null)
            throw new Exception("Not initialized");

        await Task.Delay(TimeSpan.FromSeconds(5));
        await Gated_DisconnectAsync();
        await OnDisconnectedAsync(LifeCycleCt.Value);
        await Gated_ConnectAsync();
        await OnRestoreSubscriptionsAsync(LifeCycleCt.Value);

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Reconnecting FabricClient DONE", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    private async Task DisconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError(
                "Disconnecting FabricClient {Id}",
                FabricConnectionId);

        await ConnectingGate.WaitAsync();
        try
        {
            RunningRequested = false;
            await Gated_DisconnectAsync();
        }
        finally
        {
            ConnectingGate.Release();
        }
    }
    private async Task Gated_DisconnectAsync()
    {
        if (ConnectedCts == null)
            return;

        await ConnectedCts.CancelAsync();

        Ws?.Dispose();
        Ws = null;

        ConnectedCts.Dispose();
        ConnectedCts = null;
    }


    //private async Task SendDisconnectToClients(CancellationToken ct)
    //{
    //    foreach (var subscribe in Subscriptions.Values)
    //    {
    //        await Sender.Send_Subscribe_ToServerAsync(subscribe, ct);
    //    }
    //}
    //public async Task Resubscribe(CancellationToken ct)
    //{
    //    foreach (var subscribe in Subscriptions.Values)
    //    {
    //        await Sender.Send_Subscribe_ToServerAsync(subscribe, ct);
    //    }
    //}

    //public async Task ForceReconnectAsync(CancellationToken ct)
    //{
    //    if (HttpClient.BaseUri == null)
    //        throw new Exception("Cannot get base url from IClientAuthenticatedHttpClient");

    //    await InitLock.WaitAsync(ct);
    //    try
    //    {
    //        // 1. Stop bestaande kernels
    //        try
    //        {
    //            ConnectedCts?.Cancel();
    //        }
    //        catch { }

    //        await DisposeStreamingRequestHandlersAsync();

    //        // 🎯 NIEUW: Maak de verzendwachtrij direct leeg!
    //        // Oude, niet-verzonden berichten mogen NOOIT over de nieuwe socket gaan.
    //        Sender.ClearQueue();

    //        // Laat alle hangende verzoeken direct falen zodat de UI/aanroeper weet dat de verbinding weg is
    //        foreach (var pending in PendingSendRequests.Values)
    //            pending.TrySetException(new Exception("Verbinding verbroken vanwege netwerkwissel."));

    //        foreach (var pending in PendingInvokeRequests.Values)
    //            pending.TrySetException(new Exception("Verbinding verbroken vanwege netwerkwissel."));

    //        PendingSendRequests.Clear();
    //        PendingInvokeRequests.Clear();
    //        Timeouts.Clear();

    //        // 2. Sluit websocket netjes
    //        if (Ws != null)
    //        {
    //            try
    //            {
    //                if (Ws.State == WebSocketState.Open ||
    //                    Ws.State == WebSocketState.CloseReceived)
    //                {
    //                    await Ws.CloseAsync(
    //                        WebSocketCloseStatus.NormalClosure,
    //                        "Force reconnect",
    //                        CancellationToken.None);
    //                }
    //            }
    //            catch { }

    //            Ws.Dispose();
    //            Ws = null!;
    //        }

    //        // 3. Reset state
    //        Initialized = false;
    //        InitializeTask = null;

    //        // 4. Nieuwe CTS maken
    //        ConnectedCts?.Dispose();
    //        ConnectedCts = new CancellationTokenSource();
    //    }
    //    finally
    //    {
    //        InitLock.Release();
    //    }

    //    // 5. Opnieuw verbinden via bestaande flow
    //    await TryConnectAsync(ct);
    //}


    #endregion

    #region Sender

    public async Task Send_Subscribe_ToServerAsync(object implementation, SubscribeDto subscribe, CancellationToken ct)
    {
        if (Initialized == false)
            await ConnectAsync();

        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now}: Send_Subscribe_ToServerAsync({subscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe);

        await Sender.Send_Subscribe_ToServerAsync(subscribe, ct);

        if (implementation is IHubServiceSubscription implementationSubscription)
        {
            await implementationSubscription.OnSubscribedAsync(subscribe.ServiceId, ct);
        }
    }
    public async Task Send_Unsubscribe_ToServerAsync(object implementation, UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now}: Send_Unsubscribe_ToServerAsync({unsubscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe);

        await Sender.Send_Unsubscribe_ToServerAsync(unsubscribe, ct);

        if (implementation is IHubServiceSubscription implementationSubscription)
        {
            await implementationSubscription.OnUnsubscribedAsync(unsubscribe.ServiceId, ct);
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
    private async Task ReceiverKernel(WebSocket socket, CancellationToken ct)
    {
        //bool explicitClose = false;

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
                        RunningRequested = false; // Netjes afgesloten door server of client
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
                        var synchronizeClientIds = span.ReadSynchronizeClientIdsDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_SynchronizeClientIds_FromServer(synchronizeClientIds, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricSendAndForgetRequest:
                        var sendAndForgetRequest = span.ReadSendRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricSendAndForgetRequest_FromServer(sendAndForgetRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricSendAndForgetRequestCancelled:
                        var sendAndForgetRequestCancelled = span.ReadSendRequestCancelledClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricSendAndForgetRequestCancelled_FromServer(sendAndForgetRequestCancelled, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricSendRequest:
                        var sendArgumentedRequest = span.ReadSendRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricSendRequest_FromServer(sendArgumentedRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricSendRequestCancelled:
                        var sendRequestCancelled = span.ReadSendRequestCancelledClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricSendRequestCancelled_FromServer(sendRequestCancelled, ct);
                        break;
                    case WssServerToClientMessageEnum.SendRequestDone:
                        var sendArgumentedRequestDone = span.ReadSendRequestDoneClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_SendRequestDone_FromServer(sendArgumentedRequestDone, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricInvokeRequest:
                        var invokeRequest = span.ReadInvokeRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricInvokeRequest_FromServerAsync(invokeRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricInvokeRequestCancelled:
                        var invokeRequestCancelled = span.ReadInvokeRequestCancelledClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricInvokeRequestCancelled_FromServerAsync(invokeRequestCancelled, ct);
                        break;
                    case WssServerToClientMessageEnum.InvokeRequestAck:
                        var invokeRequestAck = span.ReadInvokeRequestAckClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_InvokeRequestAck_FromServerAsync(invokeRequestAck, ct);
                        break;
                    case WssServerToClientMessageEnum.InvokeRequestDone:
                        var invokeRequestDone = span.ReadInvokeRequestDoneClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_InvokeRequestDone_FromServerAsync(invokeRequestDone, ct);
                        break;

                    case WssServerToClientMessageEnum.StreamingRequest:
                        var argumentRequest = span.ReadStreamingRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_StreamingRequest_FromServerAsync(argumentRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.StreamingResponse:
                        var argumentResponse = span.ReadStreamingResponseClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_StreamingResponse_FromServerAsync(argumentResponse, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricStreamingRequest:
                        var fabricArgumentRequest = span.ReadStreamingRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricStreamingRequest_FromServerAsync(fabricArgumentRequest, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricStreamingResponse:
                        var fabricArgumentResponse = span.ReadStreamingResponseClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        await Received_FabricStreamingResponse_FromServerAsync(fabricArgumentResponse, ct);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("ReceiverKernel => Exception: {ex}", ex);
        }
        finally
        {
            try
            {
                await DisposeStreamingRequestHandlersAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to dispose streaming request handlers after the WSS connection closed.");
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

    private async Task Received_FabricSendAndForgetRequest_FromServer(SendRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SendAndForgetRequest_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var cts = new CancellationTokenSourceDispose(message.Routing.Timeout, ct);
            Timeouts[message.Routing.RequestId] = cts;

            try
            {
                if (message.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(message.StateData, cts.Token);

                await Send_SendRequest_ToServiceAsync(message, cts.Token);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing SendAndForgetRequest for {requestId}", message.Routing.RequestId);
            }
            finally
            {
                cts.Dispose();
                Timeouts.TryRemove(message.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Received_FabricSendAndForgetRequestCancelled_FromServer(SendRequestCancelledClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SendAndForgetRequest_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (Timeouts.TryRemove(message.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }, ct);
    }


    private async Task Received_FabricSendRequest_FromServer(SendRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_SendRequest_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var cts = new CancellationTokenSourceDispose(message.Routing.Timeout, ct);
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

            var cts = new CancellationTokenSourceDispose(
                message.Routing.Timeout,
                ct);
            Timeouts[message.Routing.RequestId] = cts;



            try
            {
                if (message.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(message.StateData, cts.Token);

                var enumerable = Send_InvokeRequest_ToServiceAsync(message, cts.Token);
                RegisterAsyncEnumerableArgumentByte(message.Routing, -1, enumerable, cts.Token);

                cts.RegisterDispose(async () =>
                {
                    await UnRegisterAsyncEnumerableArgument(message.Routing, -1);
                });

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_InvokeRequestAck_ToServerAsync(
                    new InvokeRequestAckClientDto(
                        message.Routing,
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
            await Sender.Send_InvokeRequestAck_ToServerAsync(
                new InvokeRequestAckClientDto(
                    message.Routing,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Received_InvokeRequestAck_FromServerAsync(InvokeRequestAckClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_InvokeRequestAck_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (PendingInvokeRequests.TryRemove(message.Routing.RequestId, out var channel))
                channel.TrySetResult(message);
        }, ct);
    }
    private async Task Received_InvokeRequestDone_FromServerAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_InvokeRequestDone_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (PendingInvokeRequests.TryRemove(message.Routing.RequestId, out var channel))
            {
                if (message.Cancelled)
                    channel.SetCanceled(ct);
                else if (message.ExceptionMessage != null)
                    channel.SetException(new RemoteException(message.ExceptionMessage));
                else
                    channel.SetResult(new(message.Routing, message.StateIsChanged, message.StateData));
            }
        }, ct);
    }

    private async Task Received_StreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_StreamingRequest_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.CancelAfter(message.Routing.Timeout);

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
    private async Task Received_StreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_StreamingResponse_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.CancelAfter(message.Routing.Timeout);

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
                timeout.CancelAfter(message.Routing.Timeout);

            if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(message.StreamId, message.Cancelled, ct);
                await Sender.Send_FabricStreamingResponse_ToServerAsync(response, ct);
            }
        }, ct);
    }
    private async Task Received_FabricStreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Received_FabricStreamingResponse_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.CancelAfter(message.Routing.Timeout);

            if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
                responseHandler(message);
        }, ct);
    }



    #endregion

    #region Calls naar de service

    // Callbacks from fabric (to the actual services)
    public abstract Task OnDisconnectedAsync(CancellationToken lifeCycleCt);
    public abstract Task OnRestoreSubscriptionsAsync(CancellationToken lifeCycleCt);

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

            await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

            ct.Register(async () =>
            {
                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
                await Sender.Send_SendRequestCancelled_ToServerAsync(new(routing, "Task is cancelled", stateIsChanged, stateData), CancellationToken.None);
            });
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("FireAndForget chunk afgebroken vanwege time-out (30s).");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fout opgevangen in FireAndForget streaming bus-call");
            throw;
        }
    }
    public async Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        var completion = PendingSendRequests.GetOrAdd(
            routing.RequestId,
            _ => new TaskCompletionSource<SendRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously));

        try
        {
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var sendRequest = new SendRequestClientDto(routing, data, stateIsChanged, stateData);

            await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

            ct.Register(async () =>
            {
                var requestCancelled = new SendRequestCancelledClientDto(routing, "Task is cancelled", false, null);
                await Sender.Send_SendRequestCancelled_ToServerAsync(requestCancelled, ct);
                PendingSendRequests.TryRemove(routing.RequestId, out _);
            });

            var response = await completion.Task.WaitAsync(routing.Timeout, ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    ct);

            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("FireAndForget chunk afgebroken vanwege time-out (30s).");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fout opgevangen in FireAndForget streaming bus-call");
            throw;
        }
        finally
        {
            PendingSendRequests.TryRemove(routing.RequestId, out _);
        }
    }
    public async IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, [EnumeratorCancellation] CancellationToken ct)
    {
        var completion = new TaskCompletionSource<InvokeRequestAckClientDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (PendingInvokeRequests.TryAdd(routing.RequestId, completion) == false)
        {
            throw new Exception("Cache");
        }

        try
        {
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var invokeRequest = new InvokeRequestClientDto(routing, data, stateIsChanged, stateData);

            await Sender.Send_InvokeRequest_ToServerAsync(invokeRequest, ct);

            ct.Register(async () =>
            {
                var invokeCancelled = new InvokeRequestCancelledClientDto(routing, "Task is cancelled", false, null);
                await Sender.Send_InvokeRequestCancelled_ToServerAsync(invokeCancelled, ct);
                PendingInvokeRequests.TryRemove(routing.RequestId, out _);
            });

            var response = await completion.Task.WaitAsync(routing.Timeout, ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    ct);

            var list = RegisterRemoteAsyncEnumerableArgumentByte(routing, -1);
            await foreach (var item in list)
            {
                yield return item;
            }
        }
        finally
        {
            PendingInvokeRequests.TryRemove(routing.RequestId, out _);
        }
    }

    public void RegisterTimeout(RoutingDto routing, CancellationTokenSourceDispose cts)
    {
    }
    public void UnRegisterTimeout(RoutingDto routing, CancellationTokenSourceDispose cts)
    {
    }

    protected IAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({serviceId}, {methodId}, {requestId}, {argumentIndex})",
                routing.ServiceId, routing.MethodId, routing.RequestId,
                argumentIndex);

        return new RemoteAsyncEnumerable<byte[]>(
            timeOut: routing.Timeout,
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
            timeOut: routing.Timeout,
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

        StreamingRequestHandlers.TryAdd(
            new(routing.RequestId, argumentIndex),
            new AsyncEnumerableRegistration<T>(
                async (
                    ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> activeStreams,
                    StreamId streamId,
                    bool cancelled,
                    CancellationToken ct) =>
        {
            var stream = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return new AsyncEnumerableRegistrationInstance<T>(
                        source.GetAsyncEnumerator(linked.Token),
                        new SemaphoreSlim(1, 1),
                        linked);
                });
            var enumerator = stream.enumerator;
            var gate = stream.gate;
            var entered = false;

            async Task Cleanup()
            {
                stream.TryRemoveFrom(activeStreams, streamId);
                await stream.DisposeAsync(entered);
            }

            try
            {
                await gate.WaitAsync(ct);
                entered = true;

                var hasNext = !cancelled && !stream.IsDisposed && await enumerator.MoveNextAsync();

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
            var stream = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return new AsyncEnumerableRegistrationInstance<byte[]>(
                        source.GetAsyncEnumerator(linked.Token),
                        new SemaphoreSlim(1, 1),
                        linked);
                });
            var enumerator = stream.enumerator;
            var gate = stream.gate;
            var entered = false;

            async Task Cleanup()
            {
                stream.TryRemoveFrom(activeStreams, streamId);
                await stream.DisposeAsync(entered);
            }

            try
            {
                await gate.WaitAsync(ct);
                entered = true;

                var hasNext = !cancelled && !stream.IsDisposed && await enumerator.MoveNextAsync();

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

    private async Task DisposeStreamingRequestHandlersAsync()
    {
        foreach (var entry in StreamingRequestHandlers.ToArray())
        {
            if (!StreamingRequestHandlers.TryRemove(entry.Key, out var registration))
                continue;

            try
            {
                await registration.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to dispose streaming request {RequestId} argument {ArgumentIndex}.", entry.Key.RequestId, entry.Key.ArgumentIndex);
            }
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
    public Task Send_Log_ToServerAsync(ClientLogDto log, CancellationToken ct)
    {
        return Sender.Send_Log_ToServerAsync(log, ct);
    }

    #endregion

    public void Dispose()
    {
        ConnectedCts?.Cancel();
        ConnectedCts?.Dispose();
        ConnectedCts = null;
        Ws?.Dispose();
        Ws = null;
        GC.SuppressFinalize(this);
    }
}
