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
        _ = Task.Run(async () =>
        {
            try
            {
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Gated_ConnectAsync");
            }
        });
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
                    try
                    {
                        await Task.WhenAny(senderTask, receiverTask);
                        connectedCts.Cancel();
                        await Task.WhenAll(senderTask, receiverTask);

                        if (RunningRequested)
                            await ReconnectAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Gated_ConnectAsync");
                    }
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
                catch (Exception ex2)
                {
                    Logger.LogError(ex2, "Gated_ConnectAsync catch");
                }
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

        foreach (var list in ServiceSubscriptionIds.Values)
        {
            list.Clear();
        }

        await ConnectedCts.CancelAsync();

        Ws?.Dispose();
        Ws = null;

        ConnectedCts.Dispose();
        ConnectedCts = null;
    }

    #endregion

    #region Sender

    readonly ConcurrentDictionary<SubscribeDto, TaskCompletionSource<SubscribeAckDto>> SubscribeTcss = [];
    readonly ConcurrentDictionary<UnsubscribeDto, TaskCompletionSource<UnsubscribeAckDto>> UnsubscribeTcss = [];
    readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<object, ServiceSubscriptionId>> ServiceSubscriptionIds = [];

    public async Task Send_Subscribe_ToServerAsync(object implementation, ServiceId serviceId, UserId userId, SessionId sessionId, CancellationToken ct)
    {
        if (Initialized == false)
            await ConnectAsync();

        var serviceSubscriptionId = ServiceSubscriptionId.New();
        var service = ServiceSubscriptionIds.GetOrAdd(serviceId, a => new ConcurrentDictionary<object, ServiceSubscriptionId>(ReferenceEqualityComparer.Instance));
        if (service.TryAdd(implementation, serviceSubscriptionId) == false)
            throw new Exception("Cache");

        var subscribe = new SubscribeDto(
            serviceSubscriptionId,
            serviceId,
            userId,
            sessionId);

        var tcs = new TaskCompletionSource<SubscribeAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (SubscribeTcss.TryAdd(subscribe, tcs) == false)
            throw new Exception("Cannot save subscribe request");

        try
        {
            await Sender.Send_Subscribe_ToServerAsync(subscribe, ct);

            var ack = await tcs.Task.WaitAsync(ct);

            if (implementation is IHubServiceSubscription implementationSubscription)
            {
                await implementationSubscription.OnSubscribedAsync(subscribe.ServiceId, ct);
            }
        }
        finally
        {
            SubscribeTcss.TryRemove(subscribe, out _);
        }
    }
    public async Task Send_Unsubscribe_ToServerAsync(object implementation, ServiceId serviceId, UserId userId, SessionId sessionId, CancellationToken ct)
    {
        //if (Initialized == false)
        //    await ConnectAsync();

        var service = ServiceSubscriptionIds.GetOrAdd(serviceId, a => new ConcurrentDictionary<object, ServiceSubscriptionId>(ReferenceEqualityComparer.Instance));
        if (service.TryRemove(implementation, out var serviceSubscriptionId) == false)
            throw new Exception("Cache");

        var unsubscribe = new UnsubscribeDto(
            serviceSubscriptionId,
            serviceId,
            userId,
            sessionId);

        var tcs = new TaskCompletionSource<UnsubscribeAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (UnsubscribeTcss.TryAdd(unsubscribe, tcs) == false)
            throw new Exception("Cannot save unsubscribe request");

        try
        {
            await Sender.Send_Unsubscribe_ToServerAsync(unsubscribe, ct);

            var ack = await tcs.Task.WaitAsync(ct);

            if (implementation is IHubServiceSubscription implementationSubscription)
            {
                await implementationSubscription.OnUnsubscribedAsync(unsubscribe.ServiceId, ct);
            }
        }
        finally
        {
            UnsubscribeTcss.TryRemove(unsubscribe, out _);
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
                        Received_SynchronizeClientIds_FromServer(synchronizeClientIds, ct);
                        break;

                    case WssServerToClientMessageEnum.SubscribeAck:
                        var subscribeAck = span.ReadSubscribeAckDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        Received_SubscribeAck_FromServer(subscribeAck, ct);
                        break;
                    case WssServerToClientMessageEnum.UnsubscribeAck:
                        var unsubscribeAck = span.ReadUnsubscribeAckDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        Received_UnsubscribeAck_FromServer(unsubscribeAck, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricSendAndForgetRequest:
                        var sendAndForgetRequest = span.ReadSendRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricSendAndForgetRequest_FromServer(sendAndForgetRequest, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricSendAndForgetRequest_FromServer({message})", sendAndForgetRequest);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricSendAndForgetRequestCancelled:
                        var sendAndForgetRequestCancelled = span.ReadSendRequestCancelledClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricSendAndForgetRequestCancelled_FromServer(sendAndForgetRequestCancelled, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricSendAndForgetRequestCancelled_FromServer({message})", sendAndForgetRequestCancelled);
                            }
                        }, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricSendRequest:
                        var sendArgumentedRequest = span.ReadSendRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricSendRequest_FromServer(sendArgumentedRequest, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricSendRequest_FromServer({message})", sendArgumentedRequest);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricSendRequestCancelled:
                        var sendRequestCancelled = span.ReadSendRequestCancelledClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricSendRequestCancelled_FromServer(sendRequestCancelled, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricSendRequestCancelled_FromServer({message})", sendRequestCancelled);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.SendRequestDone:
                        var sendArgumentedRequestDone = span.ReadSendRequestDoneClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        Received_SendRequestDone_FromServer(sendArgumentedRequestDone, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricInvokeRequest:
                        var invokeRequest = span.ReadInvokeRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricInvokeRequest_FromServerAsync(invokeRequest, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricInvokeRequest_FromServerAsync({message})", invokeRequest);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricInvokeRequestCancelled:
                        var invokeRequestCancelled = span.ReadInvokeRequestCancelledClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricInvokeRequestCancelled_FromServerAsync(invokeRequestCancelled, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricInvokeRequestCancelled_FromServerAsync({message})", invokeRequestCancelled);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.InvokeRequestAck:
                        var invokeRequestAck = span.ReadInvokeRequestAckClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        Received_InvokeRequestAck_FromServerAsync(invokeRequestAck, ct);
                        break;
                    case WssServerToClientMessageEnum.InvokeRequestDone:
                        var invokeRequestDone = span.ReadInvokeRequestDoneClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_InvokeRequestDone_FromServerAsync(invokeRequestDone, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_InvokeRequestDone_FromServerAsync({message})", invokeRequestDone);
                            }
                        }, ct);
                        break;

                    case WssServerToClientMessageEnum.StreamingRequest:
                        var argumentRequest = span.ReadStreamingRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_StreamingRequest_FromServerAsync(argumentRequest, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_StreamingRequest_FromServerAsync({message})", argumentRequest);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.StreamingResponse:
                        var argumentResponse = span.ReadStreamingResponseClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Received_StreamingResponse_FromServerAsync(argumentResponse, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_StreamingResponse_FromServerAsync({message})", argumentResponse);
                            }
                        }, ct);
                        break;

                    case WssServerToClientMessageEnum.FabricStreamingRequest:
                        var fabricArgumentRequest = span.ReadStreamingRequestClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricStreamingRequest_FromServerAsync(fabricArgumentRequest, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricStreamingRequest_FromServerAsync({message})", fabricArgumentRequest);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricStreamingResponse:
                        var fabricArgumentResponse = span.ReadStreamingResponseClientDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Received_FabricStreamingResponse_FromServerAsync(fabricArgumentResponse, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricStreamingResponse_FromServerAsync({message})", fabricArgumentResponse);
                            }
                        }, ct);
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

    private void Received_SynchronizeClientIds_FromServer(SynchronizeClientIdsDto synchronizeClientIds, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SynchronizeClientIds_FromServer({synchronizeClientIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeClientIds);

        FabricManagerId = synchronizeClientIds.FabricManagerId;
        FabricConnectionId = synchronizeClientIds.FabricConnectionId;
        ClientConnectionId = synchronizeClientIds.ClientConnectionId;
    }

    private void Received_SubscribeAck_FromServer(SubscribeAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SubscribeAck_FromServer({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        if (SubscribeTcss.TryRemove(message.SubscribeDto, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
    }
    private void Received_UnsubscribeAck_FromServer(UnsubscribeAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_UnsubscribeAck_FromServer({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        if (UnsubscribeTcss.TryRemove(message.UnsubscribeDto, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
    }

    private async Task Received_FabricSendAndForgetRequest_FromServer(SendRequestClientDto message, CancellationToken ct)
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
    }
    private async Task Received_FabricSendAndForgetRequestCancelled_FromServer(SendRequestCancelledClientDto message, CancellationToken ct)
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
    }


    private async Task Received_FabricSendRequest_FromServer(SendRequestClientDto message, CancellationToken ct)
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
            Logger.LogError(ex, "Received_FabricSendRequest_FromServer");

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
    }
    private async Task Received_FabricSendRequestCancelled_FromServer(SendRequestCancelledClientDto message, CancellationToken ct)
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
    }
    private void Received_SendRequestDone_FromServer(SendRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SendRequestDone_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (PendingSendRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                completion.SetException(new RemoteException(message.ExceptionMessage));
            else
                completion.TrySetResult(message);
        }
    }

    private async Task Received_FabricInvokeRequest_FromServerAsync(InvokeRequestClientDto message, CancellationToken ct)
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
                    false,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Received_FabricInvokeRequest_FromServerAsync");

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
    }
    private async Task Received_FabricInvokeRequestCancelled_FromServerAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
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
                false,
                null,
                stateIsChanged,
                stateData
            ), ct);
    }
    private void Received_InvokeRequestAck_FromServerAsync(InvokeRequestAckClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_InvokeRequestAck_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (PendingInvokeRequests.TryRemove(message.Routing.RequestId, out var channel))
        {
            if (message.Cancelled)
                channel.SetCanceled(ct);
            else if (message.ExceptionMessage != null)
                channel.SetException(new RemoteException(message.ExceptionMessage));
            else
                channel.SetResult(message);
        }
    }
    private async Task Received_InvokeRequestDone_FromServerAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
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
                channel.SetResult(
                    new(message.Routing, false, null, message.StateIsChanged, message.StateData));
        }

        if (StreamingRequestHandlers.TryRemove(new(message.Routing.RequestId, -1), out var handler))
        {
            await handler.DisposeAsync();
        }
    }

    private async Task Received_StreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_StreamingRequest_FromServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            var response = await handler.Invoke(message.StreamId, message.Cancelled, ct);
            await Sender.Send_StreamingResponse_ToServerAsync(response, ct);
        }
    }
    private void Received_StreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_StreamingResponse_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
            responseHandler.Invoke(message);
    }

    private async Task Received_FabricStreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
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
    }
    private void Received_FabricStreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_FabricStreamingResponse_FromServer({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        if (Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
            responseHandler.Invoke(message);
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
    public async Task Send_FireAndForget_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose cts)
    {
        try
        {
            var ct = cts.Token;
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var sendRequest = new SendRequestClientDto(routing, data, stateIsChanged, stateData);

            await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

            cts.Register(async () =>
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
    public async Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose cts)
    {
        var completion = new TaskCompletionSource<SendRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (PendingSendRequests.TryAdd(routing.RequestId, completion) == false)
            throw new Exception("Cannot store taskcompletion");

        try
        {
            var ct = cts.Token;
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var sendRequest = new SendRequestClientDto(routing, data, stateIsChanged, stateData);

            await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

            cts.Register(async () =>
            {
                var requestCancelled = new SendRequestCancelledClientDto(routing, "Task is cancelled", false, null);
                await Sender.Send_SendRequestCancelled_ToServerAsync(requestCancelled, ct);
                PendingSendRequests.TryRemove(routing.RequestId, out _);
            });

            var response = await completion.Task.WaitAsync(routing.Timeout, ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    cts.Token);

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
    public async IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose cts)
    {
        var completion = new TaskCompletionSource<InvokeRequestAckClientDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (PendingInvokeRequests.TryAdd(routing.RequestId, completion) == false)
            throw new Exception("Cache");

        try
        {
            var ct = cts.Token;
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
        // TODO Cleanup
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
                        Logger.LogError(ex, "RegisterRemoteAsyncEnumerableArgument");
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

            var entered = false;
            async Task Cleanup()
            {
                stream.TryRemoveFrom(activeStreams, streamId);
                await stream.DisposeAsync(entered);
            }

            //if (activeStreams.TryAdd(streamId, stream) == false)
            //{
            //    await Cleanup();
            //    throw new Exception("Cache");
            //}

            var enumerator = stream.enumerator;
            var gate = stream.gate;

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
                Logger.LogError(ex, "Cancelled");

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
                Logger.LogError(ex, "RegisterAsyncEnumerableArgument");

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

            var entered = false;

            async Task Cleanup()
            {
                stream.TryRemoveFrom(activeStreams, streamId);
                await stream.DisposeAsync(entered);
            }

            //if (activeStreams.TryAdd(streamId, stream) == false)
            //{
            //    await Cleanup();
            //    throw new Exception("Cache");
            //}

            var enumerator = stream.enumerator;
            var gate = stream.gate;

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
                Logger.LogError(ex, "RegisterAsyncEnumerableArgument");

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
                Logger.LogError(ex, "RegisterAsyncEnumerableArgument");

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
