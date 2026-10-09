using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using gAPI.Core.Wss;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace gAPI.Core.Server.Wss;

public abstract class WssServerConnection : IWssServerConnection
{
    readonly ILoggerFactory LoggerFactory;
    readonly ILogger Logger;

    readonly IServerAuthenticationService AuthenticationService;
    readonly FabricClient FabricClient;
    readonly WssServerConnectionSender Sender;
    readonly ServerConnectionCollection Connections; // Doet niet veel anders dan id's
    readonly ServiceSubscriptionCollection ServiceSubscriptions; // Voor alle service subscriptions
    readonly ConcurrentDictionary<ServiceId, WssServiceSubscription> ConnectedServiceSubscriptions; // Alleen voor dispose
    readonly StreamingCache StreamingCache;
    readonly byte[] ReceiveBuffer;

    public ServerConfig Config { get; }
    public ClientConnectionId ClientConnectionId { get; }
    public FabricManagerId FabricManagerId => FabricClient.FabricManagerId;
    public FabricConnectionId FabricConnectionId => FabricClient.FabricConnectionId;

    public Stopwatch Stopwatch { get; }
    public string Id => ClientConnectionId.Value.ToString();

    public WssServerConnection(
        ServerConfig serverConfig,
        IServerAuthenticationService authenticationService,
        ServiceSubscriptionCollection serviceSubscriptions,
        ServerConnectionCollection connections,
        StreamingCache requestCache,
        FabricClient fabricClient,
        ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<WssServerConnection>();
        Config = serverConfig;
        AuthenticationService = authenticationService;
        ServiceSubscriptions = serviceSubscriptions;
        StreamingCache = requestCache;
        Connections = connections;
        FabricClient = fabricClient;
        LoggerFactory = loggerFactory;
        ConnectedServiceSubscriptions = [];
        ReceiveBuffer = new byte[serverConfig.MaxPackageSize ?? 8 * 1024 * 1024];

        ClientConnectionId = connections.AddConnection(this);
        Sender = new WssServerConnectionSender(this, loggerFactory);
        Stopwatch = Stopwatch.StartNew();
    }

    public async Task RunAsync(
        WebSocket socket,
        PathString path,
        QueryString queryString,
        IPAddress? ipAddress,
        string sessionId,
        string? cookieData,
        CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: RunAsync({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionId);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Task.WhenAll neemt params of een array van Tasks
            await Task.WhenAll(
                Sender.SendKernel(socket, cts.Token),
                ReceiverKernel(socket, path, queryString, ipAddress, sessionId, cookieData, cts)
            );
        }
        catch (TaskCanceledException)
        {
            // client disconnect of timeout — gewoon negeren
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in WssServerConnection");
            throw;
        }
    }

    #region Receiver

    private async Task ReceiverKernel(
        WebSocket socket,
        PathString path,
        QueryString queryString,
        IPAddress? ipAddress,
        string sessionId,
        string? cookieData,
        CancellationTokenSource cts)
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
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        cts.Cancel();
                        return;
                    }

                    totalBytes += result.Count;

                } while (!result.EndOfMessage);

                EnqueueReceive(totalBytes);

                // 🎯 Direct span gebruiken
                var span = new ReadOnlySpan<byte>(ReceiveBuffer, 0, totalBytes);
                int offset = 0;

                var messageType = span.ReadWssClientToServerMessageEnum(ref offset);

                //if (Logger.IsEnabled(LogLevel.Trace))
                //    Logger.LogTrace("{now}: ReceiverKernel({messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);

                try
                {
                    switch (messageType)
                    {
                        case WssClientToServerMessageEnum.Initialize:
                            var initialize = span.ReadInitializeDto(ref offset);
                            await Receive_Initialize_FromClientAsync(path, queryString, ipAddress, sessionId, cookieData, initialize, ct);
                            break;

                        case WssClientToServerMessageEnum.Subscribe:
                            var subscribe = span.ReadSubscribeDto(ref offset);
                            await Receive_Subscribe_FromClientAsync(subscribe, ct);
                            break;

                        case WssClientToServerMessageEnum.Unsubscribe:
                            var unsubscribe = span.ReadUnsubscribeDto(ref offset);
                            await Receive_Unsubscribe_FromClientAsync(unsubscribe, ct);
                            break;

                        case WssClientToServerMessageEnum.SendRequest:
                            var sendRequest = span.ReadSendRequestClientDto(ref offset);
                            await Receive_SendRequest_FromClientAsync(sendRequest, ct);
                            break;
                        case WssClientToServerMessageEnum.SendRequestCancelled:
                            var sendRequestCancelled = span.ReadSendRequestCancelledClientDto(ref offset);
                            await Receive_SendRequestCancelled_FromClientAsync(sendRequestCancelled, ct);
                            break;
                        case WssClientToServerMessageEnum.FabricSendRequestDone:
                            var fabricSendRequestDone = span.ReadSendRequestDoneClientDto(ref offset);
                            await Receive_FabricSendRequestDone_FromClientAsync(fabricSendRequestDone, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeRequest:
                            var invokeRequest = span.ReadInvokeRequestClientDto(ref offset);
                            await Receive_InvokeRequest_FromClientAsync(invokeRequest, ct);
                            break;
                        case WssClientToServerMessageEnum.InvokeRequestCancelled:
                            var invokeRequestCancelled = span.ReadInvokeRequestCancelledClientDto(ref offset);
                            await Receive_InvokeRequestCancelled_FromClientAsync(invokeRequestCancelled, ct);
                            break;
                        case WssClientToServerMessageEnum.FabricInvokeRequestDone:
                            var fabricInvokeRequestDone = span.ReadInvokeRequestDoneClientDto(ref offset);
                            await Receive_FabricInvokeRequestDone_FromClientAsync(fabricInvokeRequestDone, ct);
                            break;


                        case WssClientToServerMessageEnum.StreamingRequest:
                            var argumentRequest = span.ReadStreamingRequestClientDto(ref offset);
                            await Receive_StreamingRequest_FromClientAsync(argumentRequest, ct);
                            break;
                        case WssClientToServerMessageEnum.StreamingResponse:
                            var argumentResponse = span.ReadStreamingResponseClientDto(ref offset);
                            await Receive_StreamingResponse_FromClientAsync(argumentResponse, ct);
                            break;


                        case WssClientToServerMessageEnum.FabricStreamingRequest:
                            var fabricArgumentRequest = span.ReadStreamingRequestClientDto(ref offset);
                            await Receive_FabricStreamingRequest_FromClientAsync(fabricArgumentRequest, ct);
                            break;
                        case WssClientToServerMessageEnum.FabricStreamingResponse:
                            var fabricArgumentResponse = span.ReadStreamingResponseClientDto(ref offset);
                            await Receive_FabricStreamingResponse_FromClientAsync(fabricArgumentResponse, ct);
                            break;

                        case WssClientToServerMessageEnum.Log:
                            var log = span.ReadWssLoggerLogDto(ref offset);
                            await Receive_Log_FromClientAsync(log, ct);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing message of type {messageType} from client {ConnectionId}", messageType, ClientConnectionId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Hier komt de cancel vanuit cts.Cancel() bij disconnect, gewoon negeren
        }
    }

    private async Task Receive_Initialize_FromClientAsync(PathString path, QueryString queryString, IPAddress? ipAddress, string sessionId, string? cookieData, InitializeDto initialize, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_Initialize_FromClientAsync({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionId);

        await AuthenticationService.InitializeAsync(path, queryString, ipAddress, cookieData, sessionId, initialize.StateData, ct);
    }

    private async Task Receive_Subscribe_FromClientAsync(SubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_Subscribe_FromClientAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

        // Voor het geval dat...
        if (ConnectedServiceSubscriptions.TryRemove(message.ServiceId, out var subscription))
        {
            await subscription.DisposeAsync();
        }

        subscription = new WssServiceSubscription(
            this,
            LoggerFactory,
            ServiceSubscriptions,
            FabricClient,
            ClientConnectionId,
            message.ServiceId,
            AuthenticationService.UserId,
            AuthenticationService.SessionId);

        await FabricClient.SubscribeAsync(subscription, ct);
        ConnectedServiceSubscriptions[message.ServiceId] = subscription;
    }
    private async Task Receive_Unsubscribe_FromClientAsync(UnsubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_Unsubscribe_FromClientAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

        if (ConnectedServiceSubscriptions.TryRemove(message.ServiceId, out var subsciption))
        {
            await subsciption.DisposeAsync();
        }
    }

    private async Task Receive_SendRequest_FromClientAsync(SendRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequest_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            // Create cancellation token
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(100), ct);
            StreamingCache.Timeouts[message.Routing.RequestId] = cts;

            try
            {

                if (message.StateIsChanged)
                    await AuthenticationService.UpdateStateDataAsync(message.StateData, cts.Token);

                await AuthenticationService.UpdateStateDataAsync(message.StateData, cts.Token);
                await Send_SendRequest_ToServiceAsync(message, cts.Token);

                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_SendRequestDone_ToClientAsync(
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
                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_SendRequestDone_ToClientAsync(
                    new SendRequestDoneClientDto(
                        message.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData
                    ), ct);

                cts.Dispose();
                StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Receive_SendRequestCancelled_FromClientAsync(SendRequestCancelledClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequestCancelled_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = AuthenticationService.IsStateDataChanged();
            var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
            await Sender.Send_SendRequestDone_ToClientAsync(
                new SendRequestDoneClientDto(
                    message.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Receive_FabricSendRequestDone_FromClientAsync(SendRequestDoneClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequestDone_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            // Voor als er geen fabric is
            if (StreamingCache.PendingClientSendRequests.TryRemove(message.Routing.RequestId, out var completion))
                completion.TrySetResult(message);
            else
                await FabricClient.Send_FabricSendRequestDone_ToFabricAsync(message, ct);
        }, ct);
    }

    private async Task Receive_InvokeRequest_FromClientAsync(InvokeRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeRequest_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            // Create cancellation token
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(100), ct);
            ct = cts.Token;
            StreamingCache.Timeouts[message.Routing.RequestId] = cts;

            try
            {
                if (message.StateIsChanged)
                    await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

                // Get the enumerable
                var enumerable = Send_InvokeRequest_ToServiceAsync(message, ct);

                // Register it for streaming
                FabricClient.RegisterAsyncEnumerableArgumentByte(
                    message.Routing,
                    -1, // -1 is de response stream, min omdat hij terug gaat :) best logisch
                    enumerable,
                    ct);

                // Register dispose 
                cts.OnDispose = async () =>
                {
                    StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out _);
                    await FabricClient.UnRegisterAsyncEnumerableArgument(message.Routing, -1);
                };

                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_InvokeRequestDone_ToClientAsync(
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
                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_InvokeRequestDone_ToClientAsync(
                    new InvokeRequestDoneClientDto(
                        message.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData
                    ), ct);

                cts.Dispose();
                StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Receive_InvokeRequestCancelled_FromClientAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeCancelled_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingCache.Timeouts.TryRemove(message.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = AuthenticationService.IsStateDataChanged();
            var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
            await Sender.Send_InvokeRequestDone_ToClientAsync(
                new InvokeRequestDoneClientDto(
                    message.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Receive_FabricInvokeRequestDone_FromClientAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeRequestDone_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

            // Voor als er geen fabric is
            if (StreamingCache.PendingClientInvokeRequests.TryRemove(message.Routing.RequestId, out var completion))
                completion.TrySetResult(message);
            else
                await FabricClient.Send_FabricInvokeRequestDone_ToFabricAsync(message, ct);

            //if (FabricClient.PendingInvokeRequests.TryRemove(invokeRequestDone.Routing.RequestId, out var completion2))
            //    completion2.TrySetResult(invokeRequestDone);

            //else if (FabricClient.IsConnected)
            //    await FabricClient.Send_InvokeRequestDone_ToFabricAsync(invokeRequestDone, ct); //Moet dit?

            //StreamingCache.ArgumentRoutes.TryRemove(invokeRequestDone.Routing, out _);
        }, ct);
    }

    private async Task Receive_StreamingRequest_FromClientAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingRequest_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingCache.StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(message.StreamId, false, ct);
                await Send_StreamingResponse_ToClientAsync(response, ct);
            }
        }, ct);
    }
    private async Task Receive_StreamingResponse_FromClientAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingResponse_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingCache.Timeouts.TryGetValue(message.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingCache.StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
                responseHandler.Invoke(message);
        }, ct);
    }

    private async Task Receive_FabricStreamingRequest_FromClientAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricStreamingRequest_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

            // TODO: Misschien kunnen we hier direct doorlussen als het dezelfde server is.
            if (FabricClient.IsConnected)
            {
                // Doorlussen naar fabric
                await FabricClient.Send_StreamingRequestClientToServer_ToFabricAsync(message, ct);
            }
            else
            {
                await Receive_StreamingRequest_FromClientAsync(message, ct);
            }
        }, ct);
    }
    private async Task Receive_FabricStreamingResponse_FromClientAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricStreamingResponse_FromClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            if (message.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(message.StateData, ct);

            // TODO: Misschien kunnen we hier direct doorlussen als het dezelfde server is.
            if (FabricClient.IsConnected)
            {
                // Doorlussen naar fabric
                await FabricClient.Send_StreamingResponseClientToServer_ToFabricAsync(message, ct);
            }
            else
            {
                await Receive_StreamingResponse_FromClientAsync(message, ct);
            }
        }, ct);
    }

    private async Task Receive_Log_FromClientAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        try
        {
            if (log.Category == null) return;
            var logger = LoggerFactory.CreateLogger(log.Category);
            logger.Log(
                log.Level,
                log.Message);
        }
        catch
        {

        }
    }

    #endregion

    #region Sender

    public async Task SendRequestAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_SendRequest_ToClientAsync({sendRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequest);

        var completion = new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingClientSendRequests[sendRequest.Routing.RequestId] = completion;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(sendRequest.Routing, sendRequest.BinaryData, stateIsChanged, stateData);
        await Sender.Send_FabricSendRequest_ToClientAsync(sendRequestClient, ct);

        var cancelledFromClient = false;
        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);
            cancelledFromClient = response.Cancelled;
        }
        catch (TaskCanceledException)
        {
            stateIsChanged = AuthenticationService.IsStateDataChanged();
            stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
            await Sender.Send_FabricSendRequestCancelled_ToClientAsync(new SendRequestCancelledClientDto(sendRequest.Routing, "Task is cancelled", stateIsChanged, stateData), ct);
        }
        finally
        {
            StreamingCache.PendingClientSendRequests.TryRemove(sendRequest.Routing.RequestId, out _);
            if (cancelledFromClient)
                throw new TaskCanceledException();
        }
    }
    public async IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto invokeRequest, [EnumeratorCancellation] CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_InvokeRequest_ToClientAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

        var completion = new TaskCompletionSource<InvokeRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingClientInvokeRequests[invokeRequest.Routing.RequestId] = completion;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestClient = new InvokeRequestClientDto(invokeRequest.Routing, invokeRequest.BinaryData, stateIsChanged, stateData);
        await Sender.Send_FabricInvokeRequest_ToClientAsync(invokeRequestClient, ct);

        try
        {
            var cancelledFromClient = false;
            try
            {
                var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
                if (response.ExceptionMessage != null)
                    throw new Exception(response.ExceptionMessage);
                cancelledFromClient = response.Cancelled;
            }
            catch (TaskCanceledException)
            {
                stateIsChanged = AuthenticationService.IsStateDataChanged();
                stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_FabricInvokeRequestCancelled_ToClientAsync(new InvokeRequestCancelledClientDto(invokeRequest.Routing, "Task is cancelled", stateIsChanged, stateData), ct);
            }
            if (cancelledFromClient)
                throw new TaskCanceledException();

            var enumerable = RegisterRemoteAsyncEnumerableArgumentByte(invokeRequest.Routing, -1);
            await foreach (var item in enumerable)
                yield return item;
        }
        finally
        {
            StreamingCache.PendingClientInvokeRequests.TryRemove(invokeRequest.Routing.RequestId, out _);
        }
    }

    // Directe calls
    public Task Send_FabricSendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricSendRequest_ToClientAsync({sendRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequest);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(sendRequest.Routing, sendRequest.BinaryData, stateIsChanged, stateData);
        return Sender.Send_FabricSendRequest_ToClientAsync(sendRequestClient, ct);
    }
    public Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricInvokeRequest_ToClientAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestClient = new InvokeRequestClientDto(invokeRequest.Routing, invokeRequest.BinaryData, stateIsChanged, stateData);
        return Sender.Send_FabricInvokeRequest_ToClientAsync(invokeRequestClient, ct);
    }
    public Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricInvokeRequestCancelled_ToClientAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestCancelledClient = new InvokeRequestCancelledClientDto(cancel.Routing, cancel.Reason, stateIsChanged, stateData);
        return Sender.Send_FabricInvokeRequestCancelled_ToClientAsync(invokeRequestCancelledClient, ct);
    }
    public Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricSendRequestCancelled_ToClientAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestCancelledClient = new SendRequestCancelledClientDto(cancel.Routing, cancel.Reason, stateIsChanged, stateData);
        return Sender.Send_FabricSendRequestCancelled_ToClientAsync(invokeRequestCancelledClient, ct);
    }

    public async Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_StreamingRequest_ToClientAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingRequestClient = new StreamingRequestClientDto(
            request.Routing,
            request.ArgumentIndex,
            request.StreamId,
            request.Cancelled,
            stateIsChanged,
            stateData);
        await Sender.Send_StreamingRequest_ToClientAsync(streamingRequestClient, ct);
    }
    public async Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_StreamingResponse_ToClientAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingResponseClient = new StreamingResponseClientDto(
            response.Routing,
            response.ArgumentIndex,
            response.StreamId,
            response.IsCompleted,
            response.IsCancelled,
            response.ExceptionMessage,
            response.BinaryData,
            stateIsChanged,
            stateData);
        await Sender.Send_StreamingResponse_ToClientAsync(streamingResponseClient, ct);
    }
    public async Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricStreamingRequest_ToClientAsync({streamingRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingRequestClient = new StreamingRequestClientDto(
            request.Routing,
            request.ArgumentIndex,
            request.StreamId,
            request.Cancelled,
            stateIsChanged,
            stateData);
        await Sender.Send_FabricStreamingRequest_ToClientAsync(streamingRequestClient, ct);
    }
    public async Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto streamingResponse, CancellationToken ct)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace("{now} Send_FabricStreamingResponse_ToClientAsync({streamingResponse})", DateTime.Now.ToString("HH:mm:ss.fff"), streamingResponse);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingResponseClient = new StreamingResponseClientDto(
            streamingResponse.Routing,
            streamingResponse.ArgumentIndex,
            streamingResponse.StreamId,
            streamingResponse.IsCompleted,
            streamingResponse.IsCancelled,
            streamingResponse.ExceptionMessage,
            streamingResponse.BinaryData,
            stateIsChanged,
            stateData);
        await Sender.Send_FabricStreamingResponse_ToClientAsync(streamingResponseClient, ct);
    }

    #endregion

    #region Calls naar de service 

    protected abstract Task Send_SendRequest_ToServiceAsync(SendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(InvokeRequestDto invokeRequest, CancellationToken ct);

    #endregion

    #region Calls vanuit gegenereerde code

    protected IAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({routing})",
                routing);

        return new RemoteAsyncEnumerable<byte[]>(
            construct: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.StreamingResponseHandlers.TryAdd(key, response =>
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
                return Send_StreamingRequest_ToClientAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator) =>
            {
                return Send_StreamingRequest_ToClientAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        true),
                    default);
                //return Send_StreamingCancelled_ToClientAsync(
                //    new StreamingCancelledDto(
                //        routing,
                //        argumentIndex,
                //        streamId),
                //    default);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
            });
    }
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "{now} RegisterRemoteAsyncEnumerableArgument({routing}, {argumentIndex})",
                DateTime.Now.ToString("HH:mm:ss.fff"),
                routing,
                argumentIndex);

        return new RemoteAsyncEnumerable<T>(
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.StreamingResponseHandlers.TryAdd(key, response =>
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
                return Send_StreamingRequest_ToClientAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        false),
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
            {
                return Send_StreamingRequest_ToClientAsync(
                    new StreamingRequestDto(
                        routing,
                        argumentIndex,
                        streamId,
                        true),
                    default);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
            });
    }

    #endregion


    private readonly ConcurrentQueue<(double time, long bytes)> SendBytesLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceivedBytesLogger = new();

    private (int count, long bytes) GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        var bytes = 0L;
        var count = 0;
        foreach (var item in queue)
        {
            bytes += item.bytes;
            count++;
        }
        return new(count, bytes);
    }
    public (int count, long bytes) GetSendBytesPerSecond() => GetSpeed(SendBytesLogger);
    public (int count, long bytes) GetReceiveBytesPerSecond() => GetSpeed(ReceivedBytesLogger);
    public void EnqueueSend(long size)
    {
        SendBytesLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
    public void EnqueueReceive(long size)
    {
        ReceivedBytesLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }

    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("DisposeAsync()");

        Connections.RemoveConnection(ClientConnectionId);

        foreach (var hubHost in ConnectedServiceSubscriptions.Values)
        {
            try
            {
                await hubHost.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error while disposing hubhost {hubHost.Id}", hubHost.ServiceSubscriptionId);
            }
        }
        ConnectedServiceSubscriptions.Clear();

        GC.SuppressFinalize(this);
    }

}
