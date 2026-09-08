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
                        case WssClientToServerMessageEnum.SendRequestDone:
                            var sendArgumentedRequestDone = span.ReadSendRequestDoneClientDto(ref offset);
                            await Receive_SendRequestDone_FromClientAsync(sendArgumentedRequestDone, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeRequest:
                            var invokeRequest = span.ReadInvokeRequestClientDto(ref offset);
                            await Receive_InvokeRequest_FromClientAsync(invokeRequest, ct);
                            break;
                        case WssClientToServerMessageEnum.InvokeRequestDone:
                            var invokeResponseDone = span.ReadInvokeRequestDoneClientDto(ref offset);
                            await Receive_InvokeRequestDone_FromClientAsync(invokeResponseDone, ct);
                            break;
                        case WssClientToServerMessageEnum.InvokeRequestCancelled:
                            var invokeRequestCancelled = span.ReadInvokeRequestCancelledClientDto(ref offset);
                            await Receive_InvokeRequestCancelled_FromClientAsync(invokeRequestCancelled, ct);
                            break;

                        case WssClientToServerMessageEnum.StreamingResponse:
                            var argumentResponse = span.ReadStreamingResponseClientDto(ref offset);
                            await Receive_StreamingResponse_FromClientAsync(argumentResponse, ct);
                            break;
                        case WssClientToServerMessageEnum.StreamingRequest:
                            var argumentRequest = span.ReadStreamingRequestClientDto(ref offset);
                            await Receive_StreamingRequest_FromClientAsync(argumentRequest, ct);
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
        await AuthenticationService.InitializeAsync(path, queryString, ipAddress, cookieData, sessionId, initialize.StateData, ct);
    }

    private async Task Receive_Subscribe_FromClientAsync(SubscribeDto subscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_Subscribe_FromClientAsync({subscribe})", subscribe);

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
            Logger.LogTrace("Receive_Unsubscribe_FromClientAsync({unsubscribe})", unsubscribe);

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
                Logger.LogTrace("Receive_SendRequest_FromClientAsync({sendRequest})", sendRequest);

            // Create cancellation token
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(30), ct);
            StreamingCache.Timeouts[sendRequest.Routing.RequestId] = cts;
            
            try
            {

                if (sendRequest.StateIsChanged)
                    await AuthenticationService.UpdateStateDataAsync(sendRequest.StateData, cts.Token);

                await AuthenticationService.UpdateStateDataAsync(sendRequest.StateData, cts.Token);
                await Send_SendRequest_ToServiceAsync(sendRequest, cts.Token);
                await Sender.Send_SendRequestDone_ToClientAsync(
                    new SendRequestDoneClientDto(
                        sendRequest.Routing,
                        false,
                        null,
                        sendRequest.StateIsChanged,
                        sendRequest.StateData
                    ), ct);
            }
            catch (Exception ex)
            {
                await Sender.Send_SendRequestDone_ToClientAsync(
                    new SendRequestDoneClientDto(
                        sendRequest.Routing,
                        false,
                        ex.Message,
                        sendRequest.StateIsChanged,
                        sendRequest.StateData
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
                Logger.LogTrace("Receive_SendRequestCancelled_FromClientAsync({sendRequestCancelled})", sendRequestCancelled);

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
    private async Task Receive_SendRequestDone_FromClientAsync(SendRequestDoneClientDto sendRequestDone, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_SendRequestDone_FromClientAsync({sendRequestDone})", sendRequestDone);

            if (StreamingCache.PendingSendRequests.TryRemove(sendRequestDone.Routing.RequestId, out var completion))
                completion.TrySetResult(sendRequestDone);
            //else if (FabricClient.IsConnected)
            //    await FabricClient.Send_InvokeRequestDone_ToFabricAsync(invokeRequestDone, ct); //Moet dit?

            //StreamingCache.ArgumentRoutes.TryRemove(invokeRequestDone.Routing, out _);
        }, ct);
    }

    private async Task Receive_InvokeRequest_FromClientAsync(InvokeRequestClientDto invokeRequest, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_InvokeRequest_FromClientAsync({invokeRequest})", invokeRequest);

            // Create cancellation token
            var cts = new LinkedCancellationTokenSourceWithTimeout(TimeSpan.FromSeconds(30), ct);
            StreamingCache.Timeouts[invokeRequest.Routing.RequestId] = cts;

            try
            {
                if (invokeRequest.StateIsChanged)
                    await AuthenticationService.UpdateStateDataAsync(invokeRequest.StateData, cts.Token);

                // Get the enumerable
                var source = Send_InvokeRequest_ToServiceAsync(invokeRequest, cts.Token);

                // Register it for streaming
                FabricClient.RegisterAsyncEnumerableArgumentByte(
                    invokeRequest.Routing,
                    -1, // -1 is de response stream, min omdat hij terug gaat :) best logisch
                    source,
                    cts.Token);

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
                Logger.LogTrace("Receive_InvokeCancelled_FromClientAsync({invokeRequestCancelled})", invokeRequestCancelled);

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
    private async Task Receive_InvokeRequestDone_FromClientAsync(InvokeRequestDoneClientDto invokeRequestDone, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("Receive_InvokeRequestDone_FromClientAsync({invokeResponseDone})", invokeRequestDone);

            if (StreamingCache.PendingInvokeRequests.TryRemove(invokeRequestDone.Routing.RequestId, out var completion))
                completion.TrySetResult(invokeRequestDone);
            //else if (FabricClient.IsConnected)
            //    await FabricClient.Send_InvokeRequestDone_ToFabricAsync(invokeRequestDone, ct); //Moet dit?

            //StreamingCache.ArgumentRoutes.TryRemove(invokeRequestDone.Routing, out _);
        }, ct);
    }
    
    private async Task Receive_StreamingRequest_FromClientAsync(StreamingRequestClientDto streamingRequest, CancellationToken ct)
    {
        if (FabricClient.IsConnected)
        {
            await FabricClient.Send_StreamingRequest_ToFabricAsync(streamingRequest, ct);
        }
        else
        {
            if (await FabricClient.Handle_StreamingRequest_FromFabricAsync(streamingRequest, ct))
            {
                if (FabricClient.TryTakeStreamingResponse(
                    streamingRequest.Routing,
                    streamingRequest.ArgumentIndex,
                    streamingRequest.StreamId,
                    out var response))
                {
                    await Send_StreamingResponse_ToClientAsync(response, ct);
                }
            }
        }
    }
    private async Task Receive_StreamingResponse_FromClientAsync(StreamingResponseClientDto streamingResponse, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (StreamingCache.StreamingResponseHandlers.TryGetValue((streamingResponse.Routing.RequestId, streamingResponse.ArgumentIndex, streamingResponse.StreamId), out var responseHandler))
                responseHandler(streamingResponse);
            else if (FabricClient.IsConnected)
                await FabricClient.Send_StreamingResponse_ToFabricAsync(streamingResponse, ct);
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

    public async Task Send_SendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_SendRequest_ToClientAsync({sendRequest})",
                sendRequest);

        //StreamingCache.ArgumentRoutes[sendRequest.Routing] = 0; // TODO, is dit nodig?

        var completion = new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingSendRequests[sendRequest.Routing.RequestId] = completion;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new SendRequestClientDto(sendRequest.Routing, stateIsChanged, stateData, sendRequest.BinaryData);
        await Sender.Send_SendRequest_ToClientAsync(sendRequestClient, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
            //if (response.StateIsChanged)
            //    await AuthenticationService.UpdateStateDataAsync(response.StateData, ct);
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);
            if (response.Cancelled)
                throw new TaskCanceledException();
        }
        finally
        {
            StreamingCache.PendingSendRequests.TryRemove(sendRequest.Routing.RequestId, out _);
            //StreamingCache.ArgumentRoutes.TryRemove(sendRequest.Routing, out _);
        }
    }
    public async IAsyncEnumerable<byte[]> Send_InvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_InvokeRequest_ToClientAsync({invokeRequest})",
                invokeRequest);

        //StreamingCache.ArgumentRoutes[invokeRequest.Routing] = 0; // TODO, is dit nodig?

        var completion = new TaskCompletionSource<InvokeRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        StreamingCache.PendingInvokeRequests[invokeRequest.Routing.RequestId] = completion;

        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var sendRequestClient = new InvokeRequestClientDto(invokeRequest.Routing, invokeRequest.BinaryData, stateIsChanged, stateData);
        await Sender.Send_InvokeRequest_ToClientAsync(sendRequestClient, ct);

        try
        {
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
            //if (response.StateIsChanged)
            //    await AuthenticationService.UpdateStateDataAsync(response.StateData, ct);
            if (response.Cancelled)
                throw new TaskCanceledException();
            if (response.ExceptionMessage != null)
                throw new Exception(response.ExceptionMessage);

            try
            {
                var enumerable = RegisterRemoteAsyncEnumerableArgumentByte(invokeRequest.Routing, -1);
                await foreach (var item in enumerable)
                    yield return item;
            }
            finally
            {
                UnRegisterRemoteAsyncEnumerableArguments(invokeRequest.Routing);
            }
        }
        finally
        {
            StreamingCache.PendingSendRequests.TryRemove(invokeRequest.Routing.RequestId, out _);
            //StreamingCache.ArgumentRoutes.TryRemove(invokeRequest.Routing, out _);
        }
    }

    public async Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto streamingRequest, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingRequestClient = new StreamingRequestClientDto(
            streamingRequest.Routing,
            streamingRequest.ArgumentIndex,
            streamingRequest.StreamId,
            stateIsChanged,
            stateData);
        await Sender.Send_StreamingRequest_ToClientAsync(streamingRequestClient, ct);
    }
    public async Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto streamingResponse, CancellationToken ct)
    {
        var stateIsChanged = AuthenticationService.IsStateDataChanged();
        var stateData = stateIsChanged ? AuthenticationService.GetStateData() : null;
        var streamingResponseClient = new StreamingResponseClientDto(
            streamingResponse.Routing,
            streamingResponse.ArgumentIndex,
            streamingResponse.StreamId,
            streamingResponse.IsCompleted,
            stateIsChanged,
            stateData,
            streamingResponse.BinaryData);
        await Sender.Send_StreamingResponse_ToClientAsync(streamingResponseClient, ct);
    }



    #endregion

    #region Calls naar de service 

    protected abstract Task Send_SendRequest_ToServiceAsync(SendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(InvokeRequestDto invokeRequest, CancellationToken ct);

    #endregion

    #region Calls vanuit gegenereerde code

    protected IAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex)
    {
        return new RemoteAsyncEnumerable<byte[]>((streamId, push, complete, ct) =>
        {
            var key = (routing.RequestId, argumentIndex, streamId);
            if (!StreamingCache.StreamingResponseHandlers.ContainsKey(key))
            {
                StreamingCache.StreamingResponseHandlers[key] = response =>
                {
                    if (response.IsCompleted)
                    {
                        StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
                        complete.Invoke(null);
                    }
                    else
                    {
                        push.Invoke(response.BinaryData);
                    }
                };
            }
            return Send_StreamingRequest_ToClientAsync(
                new StreamingRequestDto(
                    routing,
                    argumentIndex,
                    streamId
                ), ct);
        });
    }
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>((streamId, push, complete, ct) =>
        {
            var key = (routing.RequestId, argumentIndex, streamId);
            if (!StreamingCache.StreamingResponseHandlers.ContainsKey(key))
            {
                StreamingCache.StreamingResponseHandlers[key] = response =>
                {
                    if (response.IsCompleted)
                    {
                        StreamingCache.StreamingResponseHandlers.TryRemove(key, out _);
                        complete.Invoke(null);
                    }
                    else
                    {
                        push.Invoke(deserializer.Invoke(response.BinaryData));
                    }
                };
            }
            return Send_StreamingRequest_ToClientAsync(
                new StreamingRequestDto(
                    routing,
                    argumentIndex,
                    streamId
                ), ct);
        });
    }
    protected void UnRegisterRemoteAsyncEnumerableArguments(RoutingDto routing)
    {

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
