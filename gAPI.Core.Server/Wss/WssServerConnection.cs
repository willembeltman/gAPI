using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using gAPI.Core.Wss;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

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
    readonly byte[] ReceiveBuffer = new byte[10 * 1024 * 1024];

    public ClientConnectionId ClientConnectionId { get; }
    public FabricManagerId FabricManagerId => FabricClient.FabricManagerId;
    public FabricConnectionId FabricConnectionId => FabricClient.FabricConnectionId;

    public WssServerConnection(
        IServerAuthenticationService authenticationService,
        ServiceSubscriptionCollection serviceSubscriptions,
        ServerConnectionCollection connections,
        StreamingCache requestCache,
        FabricClient fabricClient,
        ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<WssServerConnection>();
        AuthenticationService = authenticationService;
        ServiceSubscriptions = serviceSubscriptions;
        StreamingCache = requestCache;
        Connections = connections;
        FabricClient = fabricClient;
        LoggerFactory = loggerFactory;
        ConnectedServiceSubscriptions = [];

        ClientConnectionId = connections.AddConnection(this);
        Sender = new WssServerConnectionSender(this, loggerFactory);
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

                // 🎯 Direct span gebruiken
                var span = new ReadOnlySpan<byte>(ReceiveBuffer, 0, totalBytes);
                int offset = 0;

                var messageType = span.ReadWssClientToServerMessageEnum(ref offset);

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now}: ReceiverKernel({messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);

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
                        //case WssClientToServerMessageEnum.StreamingCancelled:
                        //    var argumentCancelled = span.ReadStreamingCancelledClientDto(ref offset);
                        //    await Receive_StreamingCancelled_FromClientAsync(argumentCancelled, ct);
                        //    break;
                        case WssClientToServerMessageEnum.StreamingResponse:
                            var argumentResponse = span.ReadStreamingResponseClientDto(ref offset);
                            await Receive_StreamingResponse_FromClientAsync(argumentResponse, ct);
                            break;


                        case WssClientToServerMessageEnum.FabricStreamingRequest:
                            var fabricArgumentRequest = span.ReadStreamingRequestClientDto(ref offset);
                            await Receive_FabricStreamingRequest_FromClientAsync(fabricArgumentRequest, ct);
                            break;
                        //case WssClientToServerMessageEnum.FabricStreamingCancelled:
                        //    var fabricArgumentCancelled = span.ReadStreamingCancelledClientDto(ref offset);
                        //    await Receive_FabricStreamingCancelled_FromClientAsync(fabricArgumentCancelled, ct);
                        //    break;
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
            Logger.LogTrace("{now}: Receive_Initialize_FromClientAsync({path}, {queryString}, {ipAddress}, {sessionId}, {cookieData}, {initialize})", DateTime.Now.ToString("HH:mm:ss.fff"), path, queryString, ipAddress, sessionId, cookieData, initialize);

        await AuthenticationService.InitializeAsync(path, queryString, ipAddress, cookieData, sessionId, initialize.StateData, ct);
    }

    private async Task Receive_Subscribe_FromClientAsync(SubscribeDto subscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_Subscribe_FromClientAsync({subscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe);

        // Voor het geval dat...
        if (ConnectedServiceSubscriptions.TryRemove(subscribe.ServiceId, out var subscription))
        {
            await subscription.DisposeAsync();
        }

        subscription = new WssServiceSubscription(
            this,
            LoggerFactory,
            ServiceSubscriptions,
            FabricClient,
            ClientConnectionId,
            subscribe.ServiceId,
            AuthenticationService.UserId,
            AuthenticationService.SessionId);

        await FabricClient.SubscribeAsync(subscription, ct);
        ConnectedServiceSubscriptions[subscribe.ServiceId] = subscription;
    }
    private async Task Receive_Unsubscribe_FromClientAsync(UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Receive_Unsubscribe_FromClientAsync({unsubscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe);

        if (ConnectedServiceSubscriptions.TryRemove(unsubscribe.ServiceId, out var subsciption))
        {
            await subsciption.DisposeAsync();
        }
    }

    private async Task Receive_SendRequest_FromClientAsync(SendRequestClientDto sendRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequest_FromClientAsync({sendRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequest);

            // Create cancellation token
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(100), ct);
            StreamingCache.Timeouts[sendRequest.Routing.RequestId] = cts;

            try
            {

                if (sendRequest.StateIsChanged)
                    await AuthenticationService.UpdateStateDataAsync(sendRequest.StateData, cts.Token);

                await AuthenticationService.UpdateStateDataAsync(sendRequest.StateData, cts.Token);
                await Send_SendRequest_ToServiceAsync(sendRequest, cts.Token);

                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_SendRequestDone_ToClientAsync(
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
                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_SendRequestDone_ToClientAsync(
                    new SendRequestDoneClientDto(
                        sendRequest.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData
                    ), ct);

                cts.Dispose();
                StreamingCache.Timeouts.TryRemove(sendRequest.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Receive_SendRequestCancelled_FromClientAsync(SendRequestCancelledClientDto sendRequestCancelled, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequestCancelled_FromClientAsync({sendRequestCancelled})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequestCancelled);

            if (sendRequestCancelled.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(sendRequestCancelled.StateData, ct);

            if (StreamingCache.Timeouts.TryRemove(sendRequestCancelled.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = AuthenticationService.IsStateDataChanged();
            var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
            await Sender.Send_SendRequestDone_ToClientAsync(
                new SendRequestDoneClientDto(
                    sendRequestCancelled.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Receive_FabricSendRequestDone_FromClientAsync(SendRequestDoneClientDto sendRequestDone, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_SendRequestDone_FromClientAsync({sendRequestDone})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequestDone);

            // Voor als er geen fabric is
            if (StreamingCache.PendingClientSendRequests.TryRemove(sendRequestDone.Routing.RequestId, out var completion))
                completion.TrySetResult(sendRequestDone);
            else
                await FabricClient.Send_FabricSendRequestDone_ToFabricAsync(sendRequestDone, ct);
        }, ct);
    }

    private async Task Receive_InvokeRequest_FromClientAsync(InvokeRequestClientDto invokeRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeRequest_FromClientAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

            // Create cancellation token
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(100), ct);
            StreamingCache.Timeouts[invokeRequest.Routing.RequestId] = cts;

            try
            {
                if (invokeRequest.StateIsChanged)
                    await AuthenticationService.UpdateStateDataAsync(invokeRequest.StateData, cts.Token);

                // Get the enumerable
                var enumerable = Send_InvokeRequest_ToServiceAsync(invokeRequest, cts.Token);

                // Register it for streaming
                FabricClient.RegisterAsyncEnumerableArgumentByte(
                    invokeRequest.Routing,
                    -1, // -1 is de response stream, min omdat hij terug gaat :) best logisch
                    enumerable,
                    cts.Token);

                // Register dispose 
                cts.OnDispose = async () =>
                {
                    StreamingCache.Timeouts.TryRemove(invokeRequest.Routing.RequestId, out _);
                    await FabricClient.UnRegisterAsyncEnumerableArgument(invokeRequest.Routing, -1);
                };

                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_InvokeRequestDone_ToClientAsync(
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
                var stateIsChanged = AuthenticationService.IsStateDataChanged();
                var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
                await Sender.Send_InvokeRequestDone_ToClientAsync(
                    new InvokeRequestDoneClientDto(
                        invokeRequest.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData
                    ), ct);

                cts.Dispose();
                StreamingCache.Timeouts.TryRemove(invokeRequest.Routing.RequestId, out _);
            }
        }, ct);
    }
    private async Task Receive_InvokeRequestCancelled_FromClientAsync(InvokeRequestCancelledClientDto invokeRequestCancelled, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeCancelled_FromClientAsync({invokeRequestCancelled})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequestCancelled);

            if (invokeRequestCancelled.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(invokeRequestCancelled.StateData, ct);

            if (StreamingCache.Timeouts.TryRemove(invokeRequestCancelled.Routing.RequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var stateIsChanged = AuthenticationService.IsStateDataChanged();
            var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
            await Sender.Send_InvokeRequestDone_ToClientAsync(
                new InvokeRequestDoneClientDto(
                    invokeRequestCancelled.Routing,
                    true,
                    null,
                    stateIsChanged,
                    stateData
                ), ct);
        }, ct);
    }
    private async Task Receive_FabricInvokeRequestDone_FromClientAsync(InvokeRequestDoneClientDto invokeRequestDone, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_InvokeRequestDone_FromClientAsync({invokeResponseDone})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequestDone);

            if (invokeRequestDone.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(invokeRequestDone.StateData, ct);

            // Voor als er geen fabric is
            if (StreamingCache.PendingClientInvokeRequests.TryRemove(invokeRequestDone.Routing.RequestId, out var completion))
                completion.TrySetResult(invokeRequestDone);
            else
                await FabricClient.Send_FabricInvokeRequestDone_ToFabricAsync(invokeRequestDone, ct);

            //if (FabricClient.PendingInvokeRequests.TryRemove(invokeRequestDone.Routing.RequestId, out var completion2))
            //    completion2.TrySetResult(invokeRequestDone);

            //else if (FabricClient.IsConnected)
            //    await FabricClient.Send_InvokeRequestDone_ToFabricAsync(invokeRequestDone, ct); //Moet dit?

            //StreamingCache.ArgumentRoutes.TryRemove(invokeRequestDone.Routing, out _);
        }, ct);
    }

    private async Task Receive_StreamingRequest_FromClientAsync(StreamingRequestClientDto streamingRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingRequest_FromClientAsync({streamingRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), streamingRequest);

            if (streamingRequest.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(streamingRequest.StateData, ct);

            if (StreamingCache.Timeouts.TryGetValue(streamingRequest.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingCache.StreamingRequestHandlers.TryGetValue(new(streamingRequest.Routing.RequestId, streamingRequest.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(streamingRequest.StreamId, false, ct);
                await Send_StreamingResponse_ToClientAsync(response, ct);
            }
        }, ct);
    }
    private async Task Receive_StreamingResponse_FromClientAsync(StreamingResponseClientDto streamingResponse, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_StreamingResponse_FromClientAsync({streamingResponse})", DateTime.Now.ToString("HH:mm:ss.fff"), streamingResponse);

            if (streamingResponse.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(streamingResponse.StateData, ct);

            if (StreamingCache.Timeouts.TryGetValue(streamingResponse.Routing.RequestId, out var timeout))
                timeout.Reset();

            if (StreamingCache.StreamingResponseHandlers.TryGetValue(new(streamingResponse.Routing.RequestId, streamingResponse.ArgumentIndex, streamingResponse.StreamId), out var responseHandler))
                responseHandler.Invoke(streamingResponse);
        }, ct);
    }

    private async Task Receive_FabricStreamingRequest_FromClientAsync(StreamingRequestClientDto streamingRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricStreamingRequest_FromClientAsync({streamingRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), streamingRequest);

            if (streamingRequest.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(streamingRequest.StateData, ct);

            // TODO: Misschien kunnen we hier direct doorlussen als het dezelfde server is.
            if (FabricClient.IsConnected)
            {
                // Doorlussen naar fabric
                await FabricClient.Send_StreamingRequestClientToServer_ToFabricAsync(streamingRequest, ct);
            }
            else
            {
                await Receive_StreamingRequest_FromClientAsync(streamingRequest, ct);
            }
        }, ct);
    }
    private async Task Receive_FabricStreamingResponse_FromClientAsync(StreamingResponseClientDto streamingResponse, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Receive_FabricStreamingResponse_FromClientAsync({streamingResponse})", DateTime.Now.ToString("HH:mm:ss.fff"), streamingResponse);

            if (streamingResponse.StateIsChanged)
                await AuthenticationService.UpdateStateDataAsync(streamingResponse.StateData, ct);

            // TODO: Misschien kunnen we hier direct doorlussen als het dezelfde server is.
            if (FabricClient.IsConnected)
            {
                // Doorlussen naar fabric
                await FabricClient.Send_StreamingResponseClientToServer_ToFabricAsync(streamingResponse, ct);
            }
        }, ct);
    }

    private async Task Receive_Log_FromClientAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        if (log.Category == null) return;
        var logger = LoggerFactory.CreateLogger(log.Category);
        logger.Log(
            log.Level,
            log.Message,
            log.Data?
                .Select(a => new KeyValuePair<string, string?>(a.Key, a.Value))
                .ToArray());
    }

    #endregion

    #region Sender

    public async Task SendRequestAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_SendRequest_ToClientAsync({sendRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequest);

        //StreamingCache.ArgumentRoutes[sendRequest.Routing] = 0; // TODO, is dit nodig?

        var completion = new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingClientSendRequests[sendRequest.Routing.RequestId] = completion;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(sendRequest.Routing, sendRequest.BinaryData, stateIsChanged, stateData);
        await Sender.Send_FabricSendRequest_ToClientAsync(sendRequestClient, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);
            if (response.Cancelled)
                throw new TaskCanceledException();
        }
        finally
        {
            StreamingCache.PendingClientSendRequests.TryRemove(sendRequest.Routing.RequestId, out _);
        }
    }
    public async IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto invokeRequest, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequest_ToClientAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

        var completion = new TaskCompletionSource<InvokeRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingClientInvokeRequests[invokeRequest.Routing.RequestId] = completion;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestClient = new InvokeRequestClientDto(invokeRequest.Routing, invokeRequest.BinaryData, stateIsChanged, stateData);
        await Sender.Send_FabricInvokeRequest_ToClientAsync(invokeRequestClient, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(100), ct);
            if (response.Cancelled)
                throw new TaskCanceledException();
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);

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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricSendRequest_ToClientAsync({sendRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequest);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(sendRequest.Routing, sendRequest.BinaryData, stateIsChanged, stateData);
        return Sender.Send_FabricSendRequest_ToClientAsync(sendRequestClient, ct);
    }
    public Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricInvokeRequest_ToClientAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestClient = new InvokeRequestClientDto(invokeRequest.Routing, invokeRequest.BinaryData, stateIsChanged, stateData);
        return Sender.Send_FabricInvokeRequest_ToClientAsync(invokeRequestClient, ct);
    }
    public Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricInvokeRequestCancelled_ToClientAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestCancelledClient = new InvokeRequestCancelledClientDto(cancel.Routing, cancel.Reason, stateIsChanged, stateData);
        return Sender.Send_FabricInvokeRequestCancelled_ToClientAsync(invokeRequestCancelledClient, ct);
    }
    public Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricSendRequestCancelled_ToClientAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var invokeRequestCancelledClient = new SendRequestCancelledClientDto(cancel.Routing, cancel.Reason, stateIsChanged, stateData);
        return Sender.Send_FabricSendRequestCancelled_ToClientAsync(invokeRequestCancelledClient, ct);
    }

    public async Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingRequest_ToClientAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

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
    public async Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingResponse_ToClientAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingResponseClient = new StreamingResponseClientDto(
            request.Routing,
            request.ArgumentIndex,
            request.StreamId,
            request.IsCompleted,
            request.IsCancelled,
            request.ExceptionMessage,
            request.BinaryData,
            stateIsChanged,
            stateData);
        await Sender.Send_StreamingResponse_ToClientAsync(streamingResponseClient, ct);
    }
    public async Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricStreamingRequest_ToClientAsync({streamingRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricStreamingResponse_ToClientAsync({streamingResponse})", DateTime.Now.ToString("HH:mm:ss.fff"), streamingResponse);

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
