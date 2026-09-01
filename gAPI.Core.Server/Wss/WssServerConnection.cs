using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Wss;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace gAPI.Core.Server.Wss;

public abstract class WssServerConnection : IWssServerConnection
{
    readonly ILoggerFactory LoggerFactory;
    readonly ILogger Logger;
    readonly WssServerConnectionCollection Connections;
    readonly IServerAuthenticationService AuthenticationService;
    readonly FabricClient FabricClient;
    readonly ConcurrentDictionary<ServiceId, WssServiceSubscription> Services;
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingSendRequests = [];
    readonly ConcurrentDictionary<RequestId, Channel<InvokeResponseDto>> PendingInvokeRequests;
    //readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Func<CancellationToken, Task>> ArgumentRequestHandlers = [];
    readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex, Guid StreamId), Action<InvokeArgumentResponseDto>> ArgumentResponseHandlers = [];
    readonly ConcurrentDictionary<RequestId, byte> ArgumentRoutes = [];
    readonly ServiceSubscriptionCollection ServiceSubscriptionCollection = new();

    private byte[] ReceiveBuffer = new byte[10 * 1024 * 1024];
    private byte[] SendBuffer = new byte[10 * 1024 * 1024];
    readonly Channel<Func<Span<byte>, int>> SendQueue = Channel.CreateUnbounded<Func<Span<byte>, int>>();

    protected abstract Task Send_SendRequest_ToServiceAsync(SendRequestDto sendRequest, CancellationToken ct);
    protected abstract Task Send_InvokeRequest_ToServiceAsync(InvokeRequestDto invokeRequest, CancellationToken ct);

    public ConnectionId ConnectionId { get; }

    public WssServerConnection(
        IServerAuthenticationService authenticationService,
        ServiceSubscriptionCollection ServiceSubscriptionCollection,
        WssServerConnectionCollection connections,
        FabricClient fabricClient,
        ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger<WssServerConnection>();
        ServiceSubscriptionCollection = ServiceSubscriptionCollection;
        Connections = connections;
        AuthenticationService = authenticationService;
        FabricClient = fabricClient;
        Services = new();
        PendingInvokeRequests = new();
        ConnectionId = connections.AddConnection(this);
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
                SendKernel(socket, cts.Token),
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
                            var sendRequest = span.ReadSendRequestDto(ref offset);
                            _ = Task.Run(async () => { await Receive_SendRequest_FromClientAsync(sendRequest, ct); }, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeRequest:
                            var invokeRequest = span.ReadInvokeRequestDto(ref offset);
                            _ = Task.Run(async () => { await Receive_InvokeRequest_FromClientAsync(invokeRequest, ct); }, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeArgumentRequest:
                            var argumentRequest = span.ReadInvokeArgumentRequestDto(ref offset);
                            _ = Task.Run(async () => { await Receive_InvokeArgumentRequest_FromClientAsync(argumentRequest, ct); }, ct);
                            break;

                        case WssClientToServerMessageEnum.SendRequestDone:
                            var sendArgumentedRequestDone = span.ReadSendRequestDoneDto(ref offset);
                            await Receive_SendRequestDone_FromClientAsync(sendArgumentedRequestDone, ct);
                            break;
                        case WssClientToServerMessageEnum.SendRequestCancelled:
                            var sendRequestCancelled = span.ReadSendRequestCancelledDto(ref offset);
                            await Receive_SendRequestCancelled_FromClientAsync(sendRequestCancelled, ct);
                            break;
                        case WssClientToServerMessageEnum.InvokeArgumentResponse:
                            var argumentResponse = span.ReadInvokeArgumentResponseDto(ref offset);
                            _ = Task.Run(async () => { await ReceiveInvokeArgumentResponse(argumentResponse, ct); }, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeArgumentCancelled:
                            var invokeArgumentCancelled = span.ReadInvokeArgumentCancelledDto(ref offset);
                            await Receive_InvokeArgumentCancelled_FromClientAsync(invokeArgumentCancelled, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeRequestCancelled:
                            var invokeRequestCancelled = span.ReadInvokeRequestCancelledDto(ref offset);
                            await Receive_InvokeRequestCancelled_FromClientAsync(invokeRequestCancelled, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeResponse:
                            var invokeResponse = span.ReadInvokeResponseDto(ref offset);
                            _ = Task.Run(async () => { await Receive_InvokeResponse_FromClientAsync(invokeResponse, ct); }, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeResponseDone:
                            var invokeResponseDone = span.ReadInvokeResponseDoneDto(ref offset);
                            _ = Task.Run(async () => { await Receive_InvokeResponseDone_FromClientAsync(invokeResponseDone, ct); }, ct);
                            break;

                        case WssClientToServerMessageEnum.Log:
                            var log = span.ReadWssLoggerLogDto(ref offset);
                            await Receive_Log_FromClientAsync(log, ct);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing message of type {messageType} from client {ConnectionId}", messageType, ConnectionId);
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
        if (Services.TryRemove(subscribe.ServiceId, out var subscription))
        {
            await subscription.DisposeAsync();
        }

        subscription = new WssServiceSubscription(
            this,
            LoggerFactory,
            ServiceSubscriptionCollection,
            FabricClient,
            ConnectionId,
            subscribe.ServiceId,
            AuthenticationService.UserId,
            AuthenticationService.SessionId);

        await subscription.InitializeAsync(ct);
        Services[subscribe.ServiceId] = subscription;
    }
    private async Task Receive_Unsubscribe_FromClientAsync(UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_Unsubscribe_FromClientAsync({unsubscribe})", unsubscribe);

        if (Services.TryRemove(unsubscribe.ServiceId, out var subsciption))
        {
            await subsciption.DisposeAsync();
        }
    }

    private async Task Receive_SendRequest_FromClientAsync(SendRequestDto sendRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_SendRequest_FromClientAsync({sendRequest})", sendRequest);

        try
        {
            await AuthenticationService.UpdateStateDataAsync(sendRequest.StateData, ct);
            await Send_SendRequest_ToServiceAsync(sendRequest, ct);
            await Send_SendRequestDone_ToClientAsync(
                new SendRequestDoneDto(
                    sendRequest.RequestId,
                    sendRequest.ServiceId,
                    sendRequest.MethodId,
                    sendRequest.UserId,
                    sendRequest.SessionId,
                    sendRequest.StateIsChanged,
                    sendRequest.StateData,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            await Send_SendRequestDone_ToClientAsync(
                new SendRequestDoneDto(
                    sendRequest.RequestId,
                    sendRequest.ServiceId,
                    sendRequest.MethodId,
                    sendRequest.UserId,
                    sendRequest.SessionId,
                    sendRequest.StateIsChanged,
                    sendRequest.StateData,
                    ex.Message
                ), ct);
        }
    }
    private async Task Receive_SendRequestDone_FromClientAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        if (PendingSendRequests.TryRemove(done.RequestId, out var completion))
        {
            completion.TrySetResult(done);
        }
    }
    private async Task Receive_SendRequestCancelled_FromClientAsync(SendRequestCancelledDto done, CancellationToken ct)
    {
        PendingSendRequests.TryRemove(done.RequestId, out _);
        ArgumentRoutes.TryRemove(done.RequestId, out _);
    }

    private async Task Receive_InvokeArgumentRequest_FromClientAsync(InvokeArgumentRequestDto argumentRequest, CancellationToken ct)
    {
        if (FabricClient.IsConnected)
        {
            await FabricClient.Sender.Send_InvokeArgumentRequest_ToFabricAsync(argumentRequest, ct);
        }
        else
        {
            if (await FabricClient.Receive_InvokeArgumentRequest_FromFabricAsync(argumentRequest, ct))
            {
                if (FabricClient.TryTakeInvokeArgumentResponse(argumentRequest.RequestId, argumentRequest.ArgumentIndex, argumentRequest.StreamId, out var response))
                    await Send_InvokeArgumentResponse_ToClientAsync(response, ct);
            }
        }
    }
    private async Task Receive_InvokeArgumentCancelled_FromClientAsync(InvokeArgumentCancelledDto cancel, CancellationToken ct)
    {
        ArgumentResponseHandlers.TryRemove((cancel.RequestId, cancel.ArgumentIndex, cancel.StreamId), out _);
    }
    private async Task ReceiveInvokeArgumentResponse(InvokeArgumentResponseDto argumentResponse, CancellationToken ct)
    {
        if (ArgumentResponseHandlers.TryGetValue((argumentResponse.RequestId, argumentResponse.ArgumentIndex, argumentResponse.StreamId), out var responseHandler))
            responseHandler(argumentResponse);
        else if (FabricClient.IsConnected)
            await FabricClient.Sender.Send_InvokeArgumentResponse_ToFabricAsync(argumentResponse, ct);
    }

    private async Task Receive_InvokeRequest_FromClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeRequest_FromClientAsync({invokeRequest})", invokeRequest);

        try
        {
            await AuthenticationService.UpdateStateDataAsync(invokeRequest.StateData, ct);
            await Send_InvokeRequest_ToServiceAsync(invokeRequest, ct);
            await Send_InvokeResponseDone_ToClientAsync(
                new InvokeResponseDoneDto(
                    invokeRequest.RequestId,
                    invokeRequest.ServiceId,
                    invokeRequest.MethodId,
                    invokeRequest.UserId,
                    invokeRequest.SessionId,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            await Send_InvokeResponseDone_ToClientAsync(
                new InvokeResponseDoneDto(
                    invokeRequest.RequestId,
                    invokeRequest.ServiceId,
                    invokeRequest.MethodId,
                    invokeRequest.UserId,
                    invokeRequest.SessionId,
                    ex.ToString()
                ), ct);
        }
    }


    private async Task Receive_InvokeRequestCancelled_FromClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        if (PendingInvokeRequests.TryRemove(cancel.RequestId, out var channel))
            channel.Writer.TryComplete(new OperationCanceledException(cancel.Reason ?? "Invoke request cancelled."));

        ArgumentRoutes.TryRemove(cancel.RequestId, out _);
    }
    private async Task Receive_InvokeResponse_FromClientAsync(InvokeResponseDto invokeResponse, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponse_FromClientAsync({invokeResponse})", invokeResponse);

        if (PendingInvokeRequests.TryGetValue(invokeResponse.RequestId, out var channel))
            channel.Writer.TryWrite(invokeResponse);
        else if (FabricClient.IsConnected)
            await FabricClient.Sender.Send_InvokeResponse_ToFabricAsync(invokeResponse, ct);
    }
    private async Task Receive_InvokeResponseDone_FromClientAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponseDone_FromClientAsync({invokeResponseDone})", invokeResponseDone);

        if (PendingInvokeRequests.TryRemove(invokeResponseDone.RequestId, out var channel))
        {
            if (invokeResponseDone.ExceptionMessage != null)
                channel.Writer.TryComplete(new Exception(invokeResponseDone.ExceptionMessage));
            else
                channel.Writer.TryComplete();
        }
        else if (FabricClient.IsConnected)
            await FabricClient.Sender.Send_InvokeResponseDone_ToFabricAsync(invokeResponseDone, ct);

        ArgumentRoutes.TryRemove(invokeResponseDone.RequestId, out _);
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

    //public async Task Send_SendRequest_ToClientAsync(WssServiceSubscription hubHost, SendRequestDto sendRequest, CancellationToken ct)
    //{
    //    ArgumentRoutes[sendRequest.RequestId] = 0;
    //    if (Logger.IsEnabled(LogLevel.Trace))
    //        Logger.LogTrace("Send_SendRequest_ToClientAsync({sendRequest})", sendRequest);

    //    // Wordt al in de gegenereerde code gedaan
    //    //sendRequest.StateData = AuthenticationService.IsStateDataChanged() ? AuthenticationService.GetStateData() : null;

    //    await EnqueueAsync(writer =>
    //    {
    //        var offset = 0;
    //        writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SendRequest);
    //        writer.Write(ref offset, sendRequest);
    //        return offset;
    //    }, ct);
    //}

    public async Task<SendRequestDoneDto> Send_SendRequest_ToClientAsync(WssServiceSubscription hubHost, SendRequestDto sendRequest, CancellationToken ct)
    {
        ArgumentRoutes[sendRequest.RequestId] = 0;
        var completion = new TaskCompletionSource<SendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        PendingSendRequests[sendRequest.RequestId] = completion;

        // Todo: Volgens mij is dit niet nodig
        //sendRequest.StateData = AuthenticationService.IsStateDataChanged() ? AuthenticationService.GetStateData() : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SendRequest);
            writer.Write(ref offset, sendRequest);
            return offset;
        }, ct);

        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
        }
        finally
        {
            PendingSendRequests.TryRemove(sendRequest.RequestId, out _);
            ArgumentRoutes.TryRemove(sendRequest.RequestId, out _);
        }
    }

    private async Task Send_SendRequestDone_ToClientAsync(SendRequestDoneDto sendRequestDone, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SendRequestDone);
            writer.Write(ref offset, sendRequestDone);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToClientAsync(WssServiceSubscription hubHost, SendRequestCancelledDto sendRequestCancelled, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SendRequestCancelled);
            writer.Write(ref offset, sendRequestCancelled);
            return offset;
        }, ct);
    }

    public bool HasRequest(RequestId requestId) => ArgumentRoutes.ContainsKey(requestId);

    public async Task Send_InvokeArgumentRequest_ToClientAsync(
        WssServiceSubscription hubHost,
        InvokeArgumentRequestDto request,
        CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeArgumentRequest);
            writer.Write(ref offset, request);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeArgumentResponse_ToClientAsync(
        WssServiceSubscription hubHost,
        InvokeArgumentResponseDto response,
        CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeArgumentResponse);
            writer.Write(ref offset, response);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeArgumentCancelled_ToClientAsync(
        WssServiceSubscription hubHost,
        InvokeArgumentCancelledDto response,
        CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeArgumentCancelled);
            writer.Write(ref offset, response);
            return offset;
        }, ct);
    }

    public async IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClientAsync(
         WssServiceSubscription hubHost,
         InvokeRequestDto invokeRequest,
         [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentRoutes[invokeRequest.RequestId] = 0;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Send_InvokeRequest_ToClientAsync({invokeRequest})",
                invokeRequest);

        var channel = Channel.CreateUnbounded<InvokeResponseDto>();
        PendingInvokeRequests[invokeRequest.RequestId] = channel;

        using var activityTimeout = new ResettableTimeout(
            timeoutDuration: TimeSpan.FromSeconds(60),
            onTimeout: () =>
            {
                Logger.LogWarning(
                    "Client did not send activity for request {RequestId}",
                    invokeRequest.RequestId);

                if (PendingInvokeRequests.TryRemove(
                    invokeRequest.RequestId,
                    out var pending))
                {
                    pending.Writer.TryComplete(
                        new TimeoutException("Client did not ACK"));
                }
            });

        // Wordt al in de gegenereerde code gedaan
        //invokeRequest.StateData =
        //    AuthenticationService.IsStateDataChanged()
        //        ? AuthenticationService.GetStateData()
        //        : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;

            writer.WriteWssServerToClientMessageEnum(
                ref offset,
                WssServerToClientMessageEnum.InvokeRequest);

            writer.Write(ref offset, invokeRequest);

            return offset;
        }, ct);

        try
        {
            await foreach (var response in channel.Reader.ReadAllAsync(ct))
            {
                // Client is alive → reset the 30 second inactivity timeout.
                activityTimeout.Reset();

                yield return response;
            }
        }
        finally
        {
            if (PendingInvokeRequests.TryRemove(
                    invokeRequest.RequestId,
                    out var pending))
            {
                pending.Writer.TryComplete();
            }
        }
    }

    public async Task Send_InvokeRequestCancelled_ToClientAsync(WssServiceSubscription hubHost, InvokeRequestCancelledDto invokeRequestCancelledDto, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeRequestCancelled);
            writer.Write(ref offset, invokeRequestCancelledDto);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeResponse_ToClientAsync(InvokeResponseDto invokeResponseDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponse_ToClientAsync({invokeResponseDto})", invokeResponseDto);

        // Wordt al in de gegenereerde code gedaan
        //invokeResponseDto.StateData = AuthenticationService.IsStateDataChanged() ? AuthenticationService.GetStateData() : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeResponse);
            writer.Write(ref offset, invokeResponseDto);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeResponseDone_ToClientAsync(InvokeResponseDoneDto invokeResponseDoneDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponseDone_ToClientAsync({invokeResponseDoneDto})", invokeResponseDoneDto);

        // Wordt al in de gegenereerde code gedaan
        //invokeResponseDoneDto.StateData = AuthenticationService.IsStateDataChanged() ? AuthenticationService.GetStateData() : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeResponseDone);
            writer.Write(ref offset, invokeResponseDoneDto);
            return offset;
        }, ct);
    }

    //public void RegisterAsyncEnumerableArgument<T>(RequestId requestId, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    //{
    //    var enumerator = source.GetAsyncEnumerator(cancellationToken);
    //    var gate = new SemaphoreSlim(1, 1);
    //    var timeout = new ResettableTimeout(TimeSpan.FromSeconds(60), () =>
    //    {
    //        ArgumentRequestHandlers.TryRemove((requestId, argumentIndex), out _);
    //        _ = enumerator.DisposeAsync().AsTask();
    //    });
    //    ArgumentRequestHandlers[(requestId, argumentIndex)] = async ct =>
    //    {
    //        timeout.Reset();
    //        await gate.WaitAsync(ct);
    //        try
    //        {
    //            var hasNext = await enumerator.MoveNextAsync();
    //            await Send_InvokeArgumentResponse_ToClientAsync(new InvokeArgumentResponseDto
    //            {
    //                RequestId = requestId,
    //                ArgumentIndex = argumentIndex,
    //                IsCompleted = !hasNext,
    //                BinaryData = hasNext ? serializer(enumerator.Current) : []
    //            }, ct);
    //            if (!hasNext)
    //            {
    //                ArgumentRequestHandlers.TryRemove((requestId, argumentIndex), out _);
    //                timeout.Dispose();
    //                await enumerator.DisposeAsync();
    //            }
    //        }
    //        finally
    //        {
    //            gate.Release();
    //        }
    //    };
    //}
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
            return Send_InvokeArgumentRequest_ToClientAsync(
                new InvokeArgumentRequestDto(
                    requestId,
                    argumentIndex,
                    streamId
                ), ct);
        });
    }
    private async Task Send_InvokeArgumentRequest_ToClientAsync(InvokeArgumentRequestDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeArgumentRequest);
            writer.Write(ref offset, request);
            return offset;
        }, ct);
    }
    private async Task Send_InvokeArgumentResponse_ToClientAsync(InvokeArgumentResponseDto response, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeArgumentResponse);
            writer.Write(ref offset, response);
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
        try
        {
            await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
            {
                var span = SendBuffer.AsSpan();

                // 🚀 direct serializen in pooled buffer
                var offset = item(span);

                // 🚀 direct versturen zonder kopie
                await socket.SendAsync(
                    new ArraySegment<byte>(SendBuffer, 0, offset),
                    WebSocketMessageType.Binary,
                    true,
                    ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Hier komt de cancel vanuit cts.Cancel() bij disconnect, gewoon negeren
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("DisposeAsync()");

        Connections.RemoveConnection(ConnectionId);

        foreach (var hubHost in Services.Values)
        {
            try
            {
                await hubHost.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error while disposing hubhost {hubHost.Id}", hubHost.Id);
            }
        }
        Services.Clear();

        GC.SuppressFinalize(this);
    }

}
