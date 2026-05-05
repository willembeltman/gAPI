using gAPI.Core.Server.Collections;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using gAPI.Core.Wss;

namespace gAPI.Core.Server.Wss;

public abstract class WssConnection : ISignalRInvoker
{
    readonly ILoggerFactory LoggerFactory;
    readonly ILogger Logger;
    readonly WssConnectionCollection Connections;
    readonly IServerAuthenticationService AuthenticationService;
    readonly FabricClient FabricClient;
    readonly ConcurrentDictionary<ServiceId, WssServiceSubscription> Services;
    readonly ConcurrentDictionary<RequestId, Channel<InvokeResponseDto>> PendingRequests;
    readonly SseHostCollection SseHostCollection;

    readonly Channel<Func<Span<byte>, int>> SendQueue = Channel.CreateUnbounded<Func<Span<byte>, int>>();

    protected abstract Task SendRequestAsync(ApiSendRequestDto sendRequest, CancellationToken ct);
    protected abstract Task InvokeRequestAsync(ApiInvokeRequestDto invokeRequest, CancellationToken ct);

    public ConnectionId ConnectionId { get; }

    public WssConnection(
        IServerAuthenticationService authenticationService,
        SseHostCollection sseHostCollection,
        WssConnectionCollection connections,
        FabricClient fabricClient,
        ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger<WssConnection>();
        SseHostCollection = sseHostCollection;
        Connections = connections;
        AuthenticationService = authenticationService;
        FabricClient = fabricClient;
        ConnectionId = connections.AddConnection(this);
        Services = new();
        PendingRequests = new();
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
            Logger.LogError(ex, "Unexpected error in WssConnection");
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
        var buffer = new byte[1024 * 64];

        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                int totalBytes = 0;
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes),
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
                var span = new ReadOnlySpan<byte>(buffer, 0, totalBytes);
                int offset = 0;

                var messageType = span.ReadWssClientToServerMessageEnum(ref offset);

                try
                {
                    switch (messageType)
                    {
                        case WssClientToServerMessageEnum.Initialize:
                            var initialize = span.ReadInitializeDto(ref offset);
                            await AuthenticationService.InitializeAsync(path, queryString, ipAddress, cookieData, sessionId, initialize.StateData, ct);
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
                            var sendRequest = span.ReadApiSendRequestDto(ref offset);
                            await Receive_SendRequest_FromClientAsync(sendRequest, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeRequest:
                            var invokeRequest = span.ReadApiInvokeRequestDto(ref offset);
                            _ = Task.Run(async () => { await Receive_InvokeRequest_FromClientAsync(invokeRequest, ct); }, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeResponse:
                            var invokeResponse = span.ReadInvokeResponseDto(ref offset);
                            await Receive_InvokeResponse_FromClientAsync(invokeResponse, ct);
                            break;

                        case WssClientToServerMessageEnum.InvokeResponseDone:
                            var invokeResponseDone = span.ReadInvokeResponseDoneDto(ref offset);
                            await Receive_InvokeResponseDone_FromClientAsync(invokeResponseDone, ct);
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

    private async Task Receive_Subscribe_FromClientAsync(SubscribeDto subscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Receive_Subscribe_FromClientAsync({subscribe})", subscribe);

        // Voor het geval dat...
        if (Services.TryRemove(subscribe.ServiceId, out var subscription))
        {
            await subscription.DisposeAsync();
        }

        subscription = new WssServiceSubscription(
            this,
            LoggerFactory,
            SseHostCollection,
            FabricClient,
            ConnectionId,
            subscribe.ServiceId,
            AuthenticationService.UserId,
            AuthenticationService.SessionId);

        await subscription.InitializeAsync(ct);
        Services[subscribe.ServiceId] = subscription;
    }
    private async Task Receive_Unsubscribe_FromClientAsync(UnsubscribeDto unsubscribe, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Receive_Unsubscribe_FromClientAsync({unsubscribe})", unsubscribe);

        if (Services.TryRemove(unsubscribe.ServiceId, out var subsciption))
        {
            await subsciption.DisposeAsync();
        }
    }

    private async Task Receive_SendRequest_FromClientAsync(ApiSendRequestDto sendRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Receive_SendRequest_FromClientAsync({sendRequest})", sendRequest);

        if (sendRequest.StateData != null)
            await AuthenticationService.UpdateStateAsync(sendRequest.StateData, ct);

        await SendRequestAsync(sendRequest, ct);
    }
    private async Task Receive_InvokeRequest_FromClientAsync(ApiInvokeRequestDto invokeRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Receive_InvokeRequest_FromClientAsync({invokeRequest})", invokeRequest);

        if (invokeRequest.StateData != null)
            await AuthenticationService.UpdateStateAsync(invokeRequest.StateData, ct);

        await InvokeRequestAsync(invokeRequest, ct);
    }

    private async Task Receive_InvokeResponse_FromClientAsync(InvokeResponseDto invokeResponse, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Receive_InvokeResponse_FromClientAsync({invokeResponse})", invokeResponse);

        if (PendingRequests.TryGetValue(invokeResponse.RequestId, out var channel))
            channel.Writer.TryWrite(invokeResponse);
    }
    private async Task Receive_InvokeResponseDone_FromClientAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Receive_InvokeResponseDone_FromClientAsync({invokeResponseDone})", invokeResponseDone);

        if (PendingRequests.TryRemove(invokeResponseDone.RequestId, out var channel))
            channel.Writer.TryComplete();
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

    public async Task Send_SendRequest_ToClientAsync(WssServiceSubscription hubHost, SendRequestDto sendRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Send_SendRequest_ToClientAsync({sendRequest})", sendRequest);

        sendRequest.StateData = AuthenticationService.IsStateChanged() ? await AuthenticationService.GetStateDataAsync(ct) : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SendRequest);
            writer.Write(ref offset, sendRequest);
            return offset;
        }, ct);
    }
    public async IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClientAsync(WssServiceSubscription hubHost, InvokeRequestDto invokeRequest, [EnumeratorCancellation] CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Send_InvokeRequest_ToClientAsync({invokeRequest})", invokeRequest);

        var channel = Channel.CreateUnbounded<InvokeResponseDto>();
        PendingRequests[invokeRequest.RequestId] = channel;

        using var activityCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var timeout = TimeSpan.FromSeconds(10);
        var activity = new SemaphoreSlim(0, 1);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!activityCts.IsCancellationRequested)
                {
                    if (!await activity.WaitAsync(timeout))
                        throw new TimeoutException("Client did not ACK");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Send_InvokeRequest_ToClientAsync => Exception: {ex}", ex);
                if (PendingRequests.TryRemove(invokeRequest.RequestId, out var pending))
                    pending.Writer.TryComplete(ex);
            }
        }, ct);

        invokeRequest.StateData = AuthenticationService.IsStateChanged() ? await AuthenticationService.GetStateDataAsync(ct) : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeRequest);
            writer.Write(ref offset, invokeRequest);
            return offset;
        }, ct);

        try
        {
            await foreach (var response in channel.Reader.ReadAllAsync(activityCts.Token))
            {
                activity.Release();
                yield return response;
            }
        }
        finally
        {
            activityCts.Cancel();
            activity.Release();

            if (PendingRequests.TryRemove(invokeRequest.RequestId, out var pending))
                pending.Writer.TryComplete();
        }
    }
    public async Task Send_InvokeResponse_ToClientAsync(ApiInvokeResponseDto invokeResponseDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Send_InvokeResponse_ToClientAsync({invokeResponseDto})", invokeResponseDto);

        invokeResponseDto.StateData = AuthenticationService.IsStateChanged() ? await AuthenticationService.GetStateDataAsync(ct) : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeResponse);
            writer.Write(ref offset, invokeResponseDto);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeResponseDone_ToClientAsync(ApiInvokeResponseDoneDto invokeResponseDoneDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " Send_InvokeResponseDone_ToClientAsync({invokeResponseDoneDto})", invokeResponseDoneDto);

        invokeResponseDoneDto.StateData = AuthenticationService.IsStateChanged() ? await AuthenticationService.GetStateDataAsync(ct) : null;

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeResponseDone);
            writer.Write(ref offset, invokeResponseDoneDto);
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
            var pool = ArrayPool<byte>.Shared;

            await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
            {
                var buffer = pool.Rent(64 * 1024); // kies een max message size
                try
                {
                    var span = buffer.AsSpan();

                    // 🚀 direct serializen in pooled buffer
                    var offset = item(span);

                    // 🚀 direct versturen zonder kopie
                    await socket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, offset),
                        WebSocketMessageType.Binary,
                        true,
                        ct);
                }
                finally
                {
                    pool.Return(buffer);
                }
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
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " DisposeAsync()");

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
