using gAPI.Core.Client.Config;
using gAPI.Core.Client.Helpers;
using gAPI.Core.Client.Interfaces;
using gAPI.Core.Client.Logs;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace gAPI.Core.Client.Wss;

public record StreamingHandler(
    bool IsCompleted,
    bool IsCancelled,
    ExceptionDto? Exception,
    byte[] BinaryData);

public abstract class WssClientConnection : IWssClientConnection
{
    public WssClientConnection(
        ILoggerFactory loggerFactory,
        ClientConfig config,
        ClientLogQueue clientLogQueue,
        IClientAuthenticatedHttpClient httpClient)
    {
        Config = config;
        ClientLogQueue = clientLogQueue;
        HttpClient = httpClient;
        Sender = new WssClientConnectionSender(this, config, loggerFactory);
        Logger = loggerFactory.CreateLogger<WssClientConnection>();
        ReceiveBuffer = new byte[config.MaxPackageSize ?? 512 * 1024];
    }

    readonly WssClientConnectionSender Sender;
    readonly ILogger<WssClientConnection> Logger;
    readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingHandler>> StreamingResponseHandlers = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneClientDto>> PendingSendRequests = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestAckClientDto>> PendingInvokeRequestAcks = [];
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneClientDto>> PendingInvokeRequestDones = [];
    readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> IncomingTimeouts = [];
    readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> OutgoingTimeouts = [];
    readonly byte[] ReceiveBuffer;

    private ClientWebSocket? Ws;

    public ClientConfig Config { get; }

    private readonly ClientLogQueue ClientLogQueue;
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
    private readonly object LifecycleLock = new();
    private CancellationTokenSource? LifecycleCts;
    private Task? ConnectionLoopTask;
    private TaskCompletionSource ConnectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource? PendingPongTcs;
    private DateTime LastReceivedUtc = DateTime.UtcNow;

    private TimeSpan KeepAliveInterval => Config.KeepAliveInterval ?? TimeSpan.FromSeconds(20);
    private TimeSpan KeepAliveTimeout => Config.KeepAliveTimeout ?? TimeSpan.FromSeconds(5);

    #region Connection

    public void Start(CancellationToken ct)
    {
        EnsureStarted(ct);
    }

    private void EnsureStarted(CancellationToken? ct = null)
    {
        lock (LifecycleLock)
        {
            if (ct.HasValue && ct.Value.CanBeCanceled)
            {
                LifeCycleCt = ct.Value;
                ct.Value.Register(() =>
                {
                    RunningRequested = false;
                    try { LifecycleCts?.Cancel(); } catch { }
                    ConnectedTcs.TrySetCanceled();
                });
            }
            else if (LifeCycleCt == null)
            {
                LifeCycleCt = CancellationToken.None;
            }

            if (RunningRequested && ConnectionLoopTask is { IsCompleted: false })
                return;

            RunningRequested = true;
            LifecycleCts?.Dispose();
            LifecycleCts = ct.HasValue && ct.Value.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(ct.Value)
                : new CancellationTokenSource();

            if (ConnectedTcs.Task.IsCompleted)
                ConnectedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var loopCt = LifecycleCts.Token;
            ConnectionLoopTask = Task.Run(async () =>
            {
                try
                {
                    await RunConnectionLoopAsync(loopCt);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "RunConnectionLoopAsync");
                }
            });
        }
    }

    public async Task WaitForConnectionAsync(CancellationToken ct)
    {
        EnsureStarted();

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            Task waitTask;
            lock (LifecycleLock)
            {
                if (IsConnected && ConnectedCts != null && !ConnectedCts.IsCancellationRequested)
                    return;

                waitTask = ConnectedTcs.Task;
            }

            await waitTask.WaitAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken ct = default)
    {
        var connectedCts = ConnectedCts;
        if (!IsConnected || connectedCts == null || connectedCts.IsCancellationRequested)
            return false;

        var pongTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref PendingPongTcs, pongTcs);

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, connectedCts.Token);
            await Sender.Send_Ping_ToServerAsync(linkedCts.Token).ConfigureAwait(false);
            await pongTcs.Task.WaitAsync(KeepAliveTimeout, linkedCts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "WSS keep-alive ping failed, triggering reconnect.");
            try { await connectedCts.CancelAsync(); } catch { }
            try { Ws?.Abort(); } catch { }
            return false;
        }
        finally
        {
            Interlocked.CompareExchange(ref PendingPongTcs, null, pongTcs);
        }
    }

    private async Task RunConnectionLoopAsync(CancellationToken lifecycleCt)
    {
        var baseUri = Config.WssBackendUrl;

        while (RunningRequested && !lifecycleCt.IsCancellationRequested)
        {
            ClientWebSocket? socket = null;
            CancellationTokenSource? connectedCts = null;
            Task? senderTask = null;
            Task? receiverTask = null;
            Task? loggerTask = null;
            Task? keepAliveTask = null;
            bool wasConnected = false;

            try
            {
                using var connectTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(lifecycleCt);
                connectTimeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

                var stateData = await HttpClient.GetStateDataAsync(false, connectTimeoutCts.Token);
                var sessionId = HttpClient.SessionId.Value;

                socket = new ClientWebSocket();
                Ws = socket;

                var url = new Uri($"{baseUri}/fabricr?SessionId={sessionId}");
                await socket.ConnectAsync(url, connectTimeoutCts.Token);

                connectedCts = CancellationTokenSource.CreateLinkedTokenSource(lifecycleCt);
                ConnectedCts = connectedCts;
                LastReceivedUtc = DateTime.UtcNow;
                wasConnected = true;

                var initialize = new InitializeDto(stateData, sessionId);
                var token = connectedCts.Token;

                senderTask = Task.Run(() => Sender.SendKernel(socket, initialize, token));
                receiverTask = Task.Run(() => ReceiverKernel(socket, token));
                loggerTask = Task.Run(() => LoggerKernel(ClientLogQueue, token));
                keepAliveTask = Task.Run(() => KeepAliveKernel(token));

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} ConnectAsync() Connected!", DateTime.Now.ToString("HH:mm:ss.fff"));

                lock (LifecycleLock)
                {
                    ConnectedTcs.TrySetResult();
                }

                // Restore subscriptions without holding any gate; bound by connectedCts.Token
                _ = RestoreSubscriptionsSafeAsync(token);

                await Task.WhenAny(senderTask, receiverTask, loggerTask, keepAliveTask);
            }
            catch (OperationCanceledException) when (lifecycleCt.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("WSS connection failed or lost, retrying: {ex}", ex.Message);
            }
            finally
            {
                lock (LifecycleLock)
                {
                    if (RunningRequested && !lifecycleCt.IsCancellationRequested)
                        ConnectedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    else
                        ConnectedTcs.TrySetCanceled();
                }

                if (connectedCts != null)
                {
                    try { await connectedCts.CancelAsync(); } catch { }
                }

                if (socket != null)
                {
                    try { socket.Abort(); } catch { }
                }

                var kernelTasks = new List<Task>(4);
                if (senderTask != null) kernelTasks.Add(senderTask);
                if (receiverTask != null) kernelTasks.Add(receiverTask);
                if (loggerTask != null) kernelTasks.Add(loggerTask);
                if (keepAliveTask != null) kernelTasks.Add(keepAliveTask);

                if (kernelTasks.Count > 0)
                {
                    try { await Task.WhenAll(kernelTasks); } catch { }
                }

                await ResetActivityAsync();

                try { socket?.Dispose(); } catch { }
                if (ReferenceEquals(Ws, socket))
                    Ws = null;

                if (ReferenceEquals(ConnectedCts, connectedCts))
                    ConnectedCts = null;
                connectedCts?.Dispose();

                if (wasConnected)
                    await OnDisconnectedSafeAsync(lifecycleCt);
            }

            if (RunningRequested && !lifecycleCt.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), lifecycleCt);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task RestoreSubscriptionsSafeAsync(CancellationToken ct)
    {
        try
        {
            await OnRestoreSubscriptionsAsync(ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "OnRestoreSubscriptionsAsync failed");
        }
    }

    private async Task OnDisconnectedSafeAsync(CancellationToken ct)
    {
        try
        {
            await OnDisconnectedAsync(ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "OnDisconnectedAsync failed");
        }
    }

    private async Task KeepAliveKernel(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(KeepAliveInterval, ct);

            if (DateTime.UtcNow - LastReceivedUtc < KeepAliveInterval)
                continue;

            var ok = await CheckConnectionAsync(ct);
            if (!ok)
                return;
        }
    }

    private async Task LoggerKernel(ClientLogQueue clientLogQueue, CancellationToken ct)
    {
        await foreach (var item in clientLogQueue.ReadAllAsync(ct))
        {
            await Sender.Send_Log_ToServerAsync(item, ct);
        }
    }

    private async Task ResetActivityAsync()
    {
        Interlocked.Exchange(ref PendingPongTcs, null)?.TrySetCanceled();

        // Eerst de timeouts laten throwen
        foreach (var key in IncomingTimeouts.Keys)
        {
            if (IncomingTimeouts.TryRemove(key, out var timeout))
            {
                timeout.Cancel();
                timeout.Dispose();
            }
        }
        foreach (var key in OutgoingTimeouts.Keys)
        {
            if (OutgoingTimeouts.TryRemove(key, out var timeout))
            {
                timeout.Cancel();
                timeout.Dispose();
            }
        }

        // Dan de activiteiten alsnog stoppen.
        foreach (var key in StreamingRequestHandlers.Keys)
        {
            if (StreamingRequestHandlers.TryRemove(key, out var handler))
            {
                await handler.DisposeAsync();
            }
        }
        foreach (var key in StreamingResponseHandlers.Keys)
        {
            if (StreamingResponseHandlers.TryRemove(key, out var handler))
            {
                handler.Invoke(new StreamingHandler(true, true, new ExceptionDto("Connection failed"), []));
            }
        }
        foreach (var key in PendingSendRequests.Keys)
        {
            if (PendingSendRequests.TryRemove(key, out var handler))
            {
                handler.TrySetCanceled();
            }
        }
        foreach (var key in PendingInvokeRequestAcks.Keys)
        {
            if (PendingInvokeRequestAcks.TryRemove(key, out var handler))
            {
                handler.TrySetCanceled();
            }
        }
        foreach (var key in PendingInvokeRequestDones.Keys)
        {
            if (PendingInvokeRequestDones.TryRemove(key, out var handler))
            {
                handler.TrySetCanceled();
            }
        }
        foreach (var key in SubscribeTcss.Keys)
        {
            if (SubscribeTcss.TryRemove(key, out var handler))
            {
                handler.TrySetCanceled();
            }
        }
        foreach (var key in UnsubscribeTcss.Keys)
        {
            if (UnsubscribeTcss.TryRemove(key, out var handler))
            {
                handler.TrySetCanceled();
            }
        }
    }

    private async Task DisconnectAsync()
    {
        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError(
                "Disconnecting FabricClient {Id}",
                FabricConnectionId);

        Task? loopTask;
        lock (LifecycleLock)
        {
            RunningRequested = false;
            try { LifecycleCts?.Cancel(); } catch { }
            ConnectedTcs.TrySetCanceled();
            loopTask = ConnectionLoopTask;
        }

        if (loopTask != null)
        {
            try { await loopTask; } catch { }
        }
    }

    #endregion

    #region Sender

    readonly ConcurrentDictionary<SubscribeDto, TaskCompletionSource<SubscribeAckDto>> SubscribeTcss = [];
    readonly ConcurrentDictionary<UnsubscribeDto, TaskCompletionSource<UnsubscribeAckDto>> UnsubscribeTcss = [];

    public async Task<ServiceSubscriptionId> Send_Subscribe_ToServerAsync(
        object implementation,
        ServiceId serviceId,
        ServiceSubscriptionId serviceSubscriptionId,
        CancellationToken ct)
    {
        var userId = HttpClient.UserId;
        var sessionId = HttpClient.SessionId;

        var message = new SubscribeDto(
            serviceSubscriptionId,
            serviceId,
            userId,
            sessionId);

        var completion = new TaskCompletionSource<SubscribeAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (SubscribeTcss.TryAdd(message, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add completion");
        }

        try
        {
            await Sender.Send_Subscribe_ToServerAsync(message, ct);

            var ack = await completion.Task.WaitAsync(ct);

            if (implementation is IHubServiceSubscription implementationSubscription)
            {
                await implementationSubscription.OnSubscribedAsync(message.ServiceId, message.ServiceSubscriptionId, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (RemoteException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Send_Subscribe_ToServerAsync({serviceId}) interrupted", serviceId);
        }
        finally
        {
            SubscribeTcss.TryRemove(message, out _);
        }

        return serviceSubscriptionId;
    }
    public async Task Send_Unsubscribe_ToServerAsync(
        object implementation,
        ServiceId serviceId,
        ServiceSubscriptionId serviceSubscriptionId,
        CancellationToken ct)
    {
        var userId = HttpClient.UserId;
        var sessionId = HttpClient.SessionId;

        var message = new UnsubscribeDto(
            serviceSubscriptionId,
            serviceId,
            userId,
            sessionId);

        var completion = new TaskCompletionSource<UnsubscribeAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (UnsubscribeTcss.TryAdd(message, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            return;
        }

        try
        {
            await Sender.Send_Unsubscribe_ToServerAsync(message, ct);

            var ack = await completion.Task.WaitAsync(ct);

            if (implementation is IHubServiceSubscription implementationSubscription)
            {
                await implementationSubscription.OnUnsubscribedAsync(message.ServiceId, message.ServiceSubscriptionId, ct);
            }
        }
        finally
        {
            UnsubscribeTcss.TryRemove(message, out _);
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
                    ct.ThrowIfCancellationRequested();

                    result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(ReceiveBuffer, totalBytes, ReceiveBuffer.Length - totalBytes),
                        ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }

                    totalBytes += result.Count;

                } while (!result.EndOfMessage);

                LastReceivedUtc = DateTime.UtcNow;

                // 🎯 Direct span gebruiken
                var span = new ReadOnlySpan<byte>(ReceiveBuffer, 0, totalBytes);
                int offset = 0;

                var messageType = span.ReadWssServerToClientMessageEnum(ref offset);

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} ReceiveKernel(messageType = {messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);

                switch (messageType)
                {
                    case WssServerToClientMessageEnum.SynchronizeClientIds:
                        var synchronizeClientIds = span.ReadSynchronizeClientIdsDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        Received_SynchronizeClientIds_FromServer(synchronizeClientIds, ct);
                        break;

                    case WssServerToClientMessageEnum.SubscribeAck:
                        var subscribeAck = span.ReadSubscribeAckDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                Received_SubscribeAck_FromServer(subscribeAck, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_SubscribeAck_FromServer({message})", subscribeAck);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.UnsubscribeAck:
                        var unsubscribeAck = span.ReadUnsubscribeAckDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                Received_UnsubscribeAck_FromServer(unsubscribeAck, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_UnsubscribeAck_FromServer({message})", unsubscribeAck);
                            }
                        }, ct);
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
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                Received_SendRequestDone_FromServer(sendArgumentedRequestDone, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_SendRequestDone_FromServer({message})", sendArgumentedRequestDone);
                            }
                        }, ct);
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
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                Received_InvokeRequestAck_FromServerAsync(invokeRequestAck, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_InvokeRequestAck_FromServerAsync({message})", invokeRequestAck);
                            }
                        }, ct);
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
                        _ = Task.Run(() =>
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
                        _ = Task.Run(() =>
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
                    case WssServerToClientMessageEnum.FabricDisconnected:
                        var fabricDisconnectedResponse = span.ReadFabricDisconnectedDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricDisconnected_FromServerAsync(fabricDisconnectedResponse, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricDisconnected_FromServerAsync({message})", fabricDisconnectedResponse);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.FabricRestoreSubscriptions:
                        var fabricRestoreSubscriptionsResponse = span.ReadFabricRestoreSubscriptionsDto(ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Received_FabricRestoreSubscriptions_FromServerAsync(fabricRestoreSubscriptionsResponse, ct);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Received_FabricRestoreSubscriptions_FromServerAsync({message})", fabricRestoreSubscriptionsResponse);
                            }
                        }, ct);
                        break;
                    case WssServerToClientMessageEnum.Pong:
                        Interlocked.Exchange(ref PendingPongTcs, null)?.TrySetResult();
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

    private void Received_SynchronizeClientIds_FromServer(SynchronizeClientIdsDto synchronizeClientIds, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SynchronizeClientIds_FromServer({synchronizeClientIds})", DateTime.Now.ToString("HH:mm:ss.fff"), synchronizeClientIds);

        FabricManagerId = synchronizeClientIds.FabricManagerId;
        FabricConnectionId = synchronizeClientIds.FabricConnectionId;
        ClientConnectionId = synchronizeClientIds.ClientConnectionId;
    }

    private void Received_SubscribeAck_FromServer(SubscribeAckDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SubscribeAck_FromServer({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        if (SubscribeTcss.TryRemove(message.SubscribeDto, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(connectionLifecycleCt);
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.SetResult(message);
        }
    }
    private void Received_UnsubscribeAck_FromServer(UnsubscribeAckDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_UnsubscribeAck_FromServer({ack})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        if (UnsubscribeTcss.TryRemove(message.UnsubscribeDto, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(connectionLifecycleCt);
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.SetResult(message);
        }
    }

    private async Task Received_FabricSendAndForgetRequest_FromServer(SendRequestClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SendAndForgetRequest_FromServer({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (message.StateIsChanged)
            await HttpClient.UpdateStateDataAsync(message.StateData, connectionLifecycleCt);

        await Send_SendAndForgetRequest_ToServiceAsync(message, connectionLifecycleCt);
    }
    private async Task Received_FabricSendAndForgetRequestCancelled_FromServer(SendRequestCancelledClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SendAndForgetRequest_FromServer({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (message.StateIsChanged)
            await HttpClient.UpdateStateDataAsync(message.StateData, connectionLifecycleCt);

        if (IncomingTimeouts.TryRemove(message.Routing.RequestId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async Task Received_FabricSendRequest_FromServer(SendRequestClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SendRequest_FromServer({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        var timeoutCts = new CancellationTokenSourceDispose(message.Routing.Timeout, connectionLifecycleCt);
        if (IncomingTimeouts.TryAdd(message.Routing.RequestId, timeoutCts) == false)
        {
            Logger.LogError("Cannot add timeout");
            return;
        }
        var ct = timeoutCts.Token;

        try
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            await Send_SendRequest_ToServiceAsync(message, timeoutCts);

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
                    new ExceptionDto(ex),
                    stateIsChanged,
                    stateData
                ), ct);

            timeoutCts.Dispose();
            IncomingTimeouts.TryRemove(message.Routing.RequestId, out _);
        }
    }
    private async Task Received_FabricSendRequestCancelled_FromServer(SendRequestCancelledClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SendRequest_FromServer({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (message.StateIsChanged)
            await HttpClient.UpdateStateDataAsync(message.StateData, connectionLifecycleCt);

        if (IncomingTimeouts.TryRemove(message.Routing.RequestId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        var ct = cts?.Token ?? connectionLifecycleCt;

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
    private void Received_SendRequestDone_FromServer(SendRequestDoneClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_SendRequestDone_FromServer({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (PendingSendRequests.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(connectionLifecycleCt);
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.SetResult(message);
        }

        if (IncomingTimeouts.TryRemove(message.Routing.RequestId, out var timeOut))
        {
            timeOut.Dispose();
        }
    }

    private async Task Received_FabricInvokeRequest_FromServerAsync(InvokeRequestClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_FabricInvokeRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        var timeoutCts = new CancellationTokenSourceDispose(
            message.Routing.Timeout,
            connectionLifecycleCt);
        if (IncomingTimeouts.TryAdd(message.Routing.RequestId, timeoutCts) == false)
        {
            Logger.LogError("Cannot add subscription");
            return;
        }
        var ct = timeoutCts.Token;

        try
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            var enumerable = Send_InvokeRequest_ToServiceAsync(message, timeoutCts, timeoutCts.Token);
            RegisterAsyncEnumerableArgumentByte(
                routing: message.Routing,
                argumentIndex: -1,
                source: enumerable,
                timeoutCts: timeoutCts,
                onDone: async () =>
                {
                    IncomingTimeouts.TryRemove(message.Routing.RequestId, out _);
                    await UnRegisterAsyncEnumerableArgument(message.Routing, -1);

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
                });

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            await Sender.Send_InvokeRequestAck_ToServerAsync(
                new InvokeRequestAckClientDto(
                    message.Routing,
                    true,
                    false,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Received_FabricInvokeRequest_FromServerAsync");

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            await Sender.Send_InvokeRequestAck_ToServerAsync(
                new InvokeRequestAckClientDto(
                    message.Routing,
                    false,
                    true,
                    new ExceptionDto(ex),
                    stateIsChanged,
                    stateData
                ), ct);

            timeoutCts.Dispose();
            IncomingTimeouts.TryRemove(message.Routing.RequestId, out _);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Received_FabricInvokeRequest_FromServerAsync");

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            await Sender.Send_InvokeRequestAck_ToServerAsync(
                new InvokeRequestAckClientDto(
                    message.Routing,
                    false,
                    false,
                    new ExceptionDto(ex),
                    stateIsChanged,
                    stateData),
                ct);

            timeoutCts.Dispose();
            IncomingTimeouts.TryRemove(message.Routing.RequestId, out _);
        }
    }
    private async Task Received_FabricInvokeRequestCancelled_FromServerAsync(InvokeRequestCancelledClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_InvokeRequestCancelled_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (message.StateIsChanged)
            await HttpClient.UpdateStateDataAsync(message.StateData, connectionLifecycleCt);

        if (IncomingTimeouts.TryRemove(message.Routing.RequestId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        var ct = cts?.Token ?? connectionLifecycleCt;

        //var stateIsChanged = HttpClient.IsStateDataChanged();
        //var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
        //await Sender.Send_InvokeRequestAck_ToServerAsync(
        //    new InvokeRequestAckClientDto(
        //        message.Routing,
        //        true,
        //        false,
        //        null,
        //        stateIsChanged,
        //        stateData
        //    ), ct);
    }
    private void Received_InvokeRequestAck_FromServerAsync(InvokeRequestAckClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_InvokeRequestAck_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (PendingInvokeRequestAcks.TryRemove(message.Routing.RequestId, out var channel))
        {
            if (message.Cancelled)
                channel.SetCanceled(connectionLifecycleCt);
            else if (message.Exception != null)
                channel.SetException(new RemoteException(message.Exception));
            else
                channel.SetResult(message);
        }
    }
    private async Task Received_InvokeRequestDone_FromServerAsync(InvokeRequestDoneClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_InvokeRequestDone_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        if (PendingInvokeRequestDones.TryRemove(message.Routing.RequestId, out var completion))
        {
            if (message.Cancelled)
                completion.SetCanceled(connectionLifecycleCt);
            else if (message.Exception != null)
                completion.SetException(new RemoteException(message.Exception));
            else
                completion.SetResult(message);
        }

        //if (StreamingRequestHandlers.TryRemove(new(message.Routing.RequestId, -1), out var handler))
        //{
        //    await handler.DisposeAsync();
        //}

        //if (IncomingTimeouts.TryRemove(message.Routing.RequestId, out var timeOut))
        //{
        //    timeOut.Dispose();
        //}
        //if (OutgoingTimeouts.TryRemove(message.Routing.RequestId, out var timeOut2))
        //{
        //    timeOut2.Dispose();
        //}
    }

    private async Task Received_StreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_StreamingRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        if (IncomingTimeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);
        var ct = timeout?.Token ?? connectionLifecycleCt;

        if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            var response = await handler.Invoke(message.StreamId, message.Cancelled, ct);
            await Sender.Send_StreamingResponse_ToServerAsync(response.response, ct);
            if (response.action != null)
            {
                await response.action.Invoke();
            }
        }
    }
    private void Received_StreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_StreamingResponse_FromServer({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        if (IncomingTimeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);
        if (OutgoingTimeouts.TryGetValue(message.Routing.RequestId, out var timeout2))
            timeout2.CancelAfter(message.Routing.Timeout);

        var key = new RequestArgumentIndexStreamDto(message.Routing.RequestId, message.ArgumentIndex, message.StreamId);
        if (StreamingResponseHandlers.TryGetValue(key, out var responseHandler))
            responseHandler.Invoke(
                new StreamingHandler(
                    message.IsCompleted,
                    message.Cancelled,
                    message.Exception,
                    message.BinaryData));
    }

    private async Task Received_FabricStreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_FabricStreamingRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        if (IncomingTimeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);
        var ct = timeout?.Token ?? connectionLifecycleCt;

        if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
        {
            var response = await handler.Invoke(message.StreamId, message.Cancelled, ct);
            await Sender.Send_FabricStreamingResponse_ToServerAsync(response.response, ct);
            if (response.action != null)
            {
                await response.action.Invoke();
            }
        }
    }
    private void Received_FabricStreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken connectionLifecycleCt)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Received_FabricStreamingResponse_FromServer({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        if (IncomingTimeouts.TryGetValue(message.Routing.RequestId, out var timeout))
            timeout.CancelAfter(message.Routing.Timeout);

        if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
            responseHandler.Invoke(
                new StreamingHandler(
                    message.IsCompleted,
                    message.IsCancelled,
                    message.Exception,
                    message.BinaryData));
    }

    private async Task Received_FabricDisconnected_FromServerAsync(FabricDisconnectedDto message, CancellationToken connectionLifecycleCt)
    {
        await OnDisconnectedAsync(connectionLifecycleCt);

        await ResetActivityAsync();
    }

    private async Task Received_FabricRestoreSubscriptions_FromServerAsync(FabricRestoreSubscriptionsDto message, CancellationToken connectionLifecycleCt)
    {
        await OnRestoreSubscriptionsAsync(connectionLifecycleCt);
    }

    #endregion

    #region Calls naar de service

    // Callbacks from fabric (to the actual services)
    public abstract Task OnDisconnectedAsync(CancellationToken lifeCycleCt);
    public abstract Task OnRestoreSubscriptionsAsync(CancellationToken lifeCycleCt);

    protected abstract Task Send_SendAndForgetRequest_ToServiceAsync(
        SendRequestDto sendRequest,
        CancellationToken ct);
    protected abstract Task Send_SendRequest_ToServiceAsync(
        SendRequestDto sendRequest,
        CancellationTokenSourceDispose timeoutCts);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(
        InvokeRequestDto invokeRequest,
        CancellationTokenSourceDispose timeoutCts,
        CancellationToken ct);

    #endregion

    #region Call's vanuit gegenereerde code

    public void RegisterTimeout(RoutingDto routing, CancellationTokenSourceDispose cts)
    {
        OutgoingTimeouts.TryAdd(routing.RequestId, cts);
    }
    public void UnRegisterTimeout(RoutingDto routing, CancellationTokenSourceDispose cts)
    {
        OutgoingTimeouts.TryRemove(routing.RequestId, out _);
    }

    public async Task Send_FireAndForget_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct)
    {
        try
        {
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var sendRequest = new SendRequestClientDto(routing, data, stateIsChanged, stateData);

            await Sender.Send_SendAndForgetRequest_ToServerAsync(sendRequest, ct);
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
    public async Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose timeoutCts)
    {
        var completion = new TaskCompletionSource<SendRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (PendingSendRequests.TryAdd(routing.RequestId, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add requestid");
        }

        try
        {
            var ct = timeoutCts.Token;
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var sendRequest = new SendRequestClientDto(routing, data, stateIsChanged, stateData);

            await Sender.Send_SendRequest_ToServerAsync(sendRequest, ct);

            timeoutCts.Register(async () =>
            {
                try
                {
                    var requestCancelled = new SendRequestCancelledClientDto(routing, "Task is cancelled", false, null);
                    await Sender.Send_SendRequestCancelled_ToServerAsync(requestCancelled, CancellationToken.None);
                    PendingSendRequests.TryRemove(routing.RequestId, out _);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Send_SendRequest_ToServerAsync");
                }
            });

            var response = await completion.Task.WaitAsync(routing.Timeout, ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    timeoutCts.Token);
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
    public async IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose timeoutCts, [EnumeratorCancellation] CancellationToken ct)
    {
        var completion = new TaskCompletionSource<InvokeRequestAckClientDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (PendingInvokeRequestAcks.TryAdd(routing.RequestId, completion) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add requestid");
        }
        var completion2 = new TaskCompletionSource<InvokeRequestDoneClientDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (PendingInvokeRequestDones.TryAdd(routing.RequestId, completion2) == false)
        {
            Logger.LogError("Cannot add completion");
            throw new Exception("Cannot add requestid");
        }

        try
        {
            //var ct = timeoutCts.Token;
            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync(false, ct) : null;
            var invokeRequest = new InvokeRequestClientDto(routing, data, stateIsChanged, stateData);

            await Sender.Send_InvokeRequest_ToServerAsync(invokeRequest, ct);

            timeoutCts.Register(async () =>
            {
                try
                {
                    var invokeCancelled = new InvokeRequestCancelledClientDto(routing, "Task is cancelled", false, null);
                    await Sender.Send_InvokeRequestCancelled_ToServerAsync(invokeCancelled, CancellationToken.None);

                    if (PendingInvokeRequestAcks.TryRemove(routing.RequestId, out var ack))
                        ack.TrySetCanceled();
                    if (PendingInvokeRequestDones.TryRemove(routing.RequestId, out var done))
                        done.TrySetCanceled();

                    completion.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Send_InvokeRequest_ToServerAsync");
                }

            });

            var response = await completion.Task.WaitAsync(routing.Timeout, ct);
            if (response.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    ct);

            if (response.EnumeratorCreated == false)
                yield break;

            var list = RegisterRemoteAsyncEnumerableArgumentByte(routing, -1, timeoutCts);
            await foreach (var item in list)
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
            }


            timeoutCts.Register(async () =>
            {
                try
                {
                    if (PendingInvokeRequestAcks.TryRemove(routing.RequestId, out var ack))
                        ack.TrySetCanceled();
                    if (PendingInvokeRequestDones.TryRemove(routing.RequestId, out var done))
                        done.TrySetCanceled();

                    completion2.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Send_InvokeRequest_ToServerAsync register");
                }
            });

            var response2 = await completion2.Task.WaitAsync(routing.Timeout, ct);
            if (response2.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    response.StateData,
                    ct);
        }
        finally
        {
            PendingInvokeRequestAcks.TryRemove(routing.RequestId, out _);
            PendingInvokeRequestDones.TryRemove(routing.RequestId, out _);
        }
    }

    protected IAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, CancellationTokenSourceDispose timeoutCts)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({serviceId}, {methodId}, {requestId}, {argumentIndex})",
                routing.ServiceId, routing.MethodId, routing.RequestId,
                argumentIndex);

        return new RemoteAsyncEnumerable<byte[]>(
            timeoutCts: timeoutCts,
            construct: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, message =>
                {
                    if (message.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(message.Exception?.Message));
                        return;
                    }

                    if (message.Exception != null)
                    {
                        enumerator.Complete(new RemoteException(message.Exception));
                        return;
                    }

                    if (message.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    enumerator.Push(message.BinaryData);
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
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, Func<byte[], T> deserializer, CancellationTokenSourceDispose timeoutCts)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterRemoteAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        return new RemoteAsyncEnumerable<T>(
            timeoutCts: timeoutCts,
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, message =>
                {
                    if (message.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(message.Exception?.Message));
                        return;
                    }

                    if (message.Exception != null)
                    {
                        enumerator.Complete(new RemoteException(message.Exception));
                        return;
                    }

                    if (message.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    try
                    {
                        enumerator.Push(deserializer.Invoke(message.BinaryData));
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

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose timeoutCts)
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
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token);
                    return new AsyncEnumerableRegistrationInstance<T>(
                        source.GetAsyncEnumerator(cts.Token),
                        new SemaphoreSlim(1, 1),
                        cts);
                });

            var entered = false;
            async Task Cleanup()
            {
                stream.TryRemoveFrom(activeStreams, streamId);
                await stream.DisposeAsync(entered);
            }
            stream.timeoutCts.Token.Register(async () =>
            {
                await Cleanup();
            });

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

                return new AsyncEnumerableRegistrationResponse(response, null);
            }
            catch (OperationCanceledException ex) when (
                ct.IsCancellationRequested ||
                timeoutCts.IsCancellationRequested)
            {
                Logger.LogError(ex, "RegisterAsyncEnumerableArgument Cancelled");

                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new AsyncEnumerableRegistrationResponse(
                    new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    new ExceptionDto(ex),
                    [],
                    stateIsChanged,
                    stateData), null);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "RegisterAsyncEnumerableArgument exception");

                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new AsyncEnumerableRegistrationResponse(
                    new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    new ExceptionDto(ex),
                    [],
                    stateIsChanged,
                    stateData), null);
            }
            finally
            {
                if (entered)
                    gate.Release();
            }
        }));
    }
    public void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationTokenSourceDispose timeoutCts, Func<Task> onDone)
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
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token);
                    return new AsyncEnumerableRegistrationInstance<byte[]>(
                        source.GetAsyncEnumerator(cts.Token),
                        new SemaphoreSlim(1, 1),
                        cts);
                });

            var entered = false;

            async Task Cleanup()
            {
                stream.TryRemoveFrom(activeStreams, streamId);
                await stream.DisposeAsync(entered);
            }
            stream.timeoutCts.Token.Register(async () =>
            {
                await Cleanup();
            });

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

                return new AsyncEnumerableRegistrationResponse(response, hasNext ? null : onDone);
            }
            catch (OperationCanceledException ex) when (
                ct.IsCancellationRequested ||
                timeoutCts.IsCancellationRequested)
            {
                Logger.LogError(ex, "RegisterAsyncEnumerableArgument cancelled");

                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new AsyncEnumerableRegistrationResponse(
                    new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    new ExceptionDto(ex),
                    [],
                    stateIsChanged,
                    stateData), onDone);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "RegisterAsyncEnumerableArgument exception");

                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new AsyncEnumerableRegistrationResponse(
                    new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    new ExceptionDto(ex),
                    [],
                    stateIsChanged,
                    stateData), onDone);
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

    public void Dispose()
    {
        lock (LifecycleLock)
        {
            RunningRequested = false;
            try { LifecycleCts?.Cancel(); } catch { }
            ConnectedTcs.TrySetCanceled();
        }
        ConnectedCts?.Cancel();
        ConnectedCts?.Dispose();
        ConnectedCts = null;
        Ws?.Dispose();
        Ws = null;
        LifecycleCts?.Dispose();
        LifecycleCts = null;
        GC.SuppressFinalize(this);
    }

}
