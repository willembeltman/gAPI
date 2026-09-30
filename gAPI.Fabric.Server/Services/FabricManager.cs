using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Config;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Models;
using gAPI.Fabric.Server.Monitoring;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Services;

public class FabricManager
{
    private readonly SessionCache SessionCache;

    public readonly FabricManagerId FabricManagerId;

    public readonly FabricConfig Config;

    public readonly FabricHostCollection Connections;
    public readonly ServiceCollection Services;
    public readonly ConcurrentDictionary<RequestId, RequestState> OpenRequests;
    public readonly ConcurrentDictionary<RequestId, ServerRequestState> OpenServerRequests;
    public readonly Service Logging;
    public readonly Service System;
    public readonly ConsoleBuffer Console;

    public FabricManager(
        FabricConfig config, 
        ConsoleBuffer consoleBuffer)
    {
        FabricManagerId = FabricManagerId.New();
        Config = config;

        SessionCache = new();
        Connections = new();
        Services = new(this);
        OpenRequests = new();
        OpenServerRequests = new();
        Console = consoleBuffer; // Initial state

        System = Services[new ServiceId("Fabric System")];
        Logging = Services[new ServiceId("Fabric Logging")];
    }

    //// Handler voor als het aantal connections of subscriptions veranderd
    //public event EventHandler? Changed;
    //public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);


    public void StartNewFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricHost = new FabricHost(this, tcpClient);
        fabricHost.Start();

        //NotifyChanged();
    }

    public async Task Receive_UpdateSession_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, UpdateSessionDto updateSession, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_UpdateSession_FromApiAsync({updateSession}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), updateSession, receiveSize);
            SessionCache.AddOrUpdate(updateSession.SessionId, updateSession.CookieData);
        }, ct);
    }
    public async Task Receive_ClearSession_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SendClearSessionDto clearSession, long receiveSize, CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_ClearSession_FromApiAsync({clearSession}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), clearSession, receiveSize);
            SessionCache.Remove(clearSession.SessionId);
        }, token);
    }
    public async Task Receive_GetSessionCookieData_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SendGetSessionCookieDataDto getSessionCookieData, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_GetSessionCookieData_FromApiAsync({getSessionCookieData}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), getSessionCookieData, receiveSize);
            var sessionId = getSessionCookieData.SessionId;
            string? cookieData = null;
            SessionCache.TryGet(sessionId, out cookieData);
            var getSessionCookieDataResponse = new SendGetSessionCookieDataResponseDto(sessionId, cookieData);
            await caller.Send_GetSessionCookieDataResponse_ToApiAsync(getSessionCookieDataResponse, System);
        }, ct);
    }

    public async Task Receive_Subscribe_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SubscribeDto subscribe, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_Subscribe_FromApiAsync({subscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe, receiveSize);

            await Services[subscribe.ServiceId]
                .Subscribe(caller, subscribe.UserId, subscribe.SessionId, receiveSize);

            //NotifyChanged();
        }, ct);
    }
    public async Task Receive_Unsubscribe_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, UnsubscribeDto unsubscribe, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_Unsubscribe_FromApiAsync({unsubscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe, receiveSize);

            await Services[unsubscribe.ServiceId]
                .Unsubscribe(caller, unsubscribe.UserId, unsubscribe.SessionId, receiveSize);

            //NotifyChanged();
        }, ct);
    }

    public async Task Receive_SendRequest_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SendRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_SendRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            (var fabricHostsEnumerable, var actor) = Services[request.Routing.ServiceId]
                .GetFabricHosts(request.Routing.UserId, request.Routing.SessionId);
            var fabricHosts = fabricHostsEnumerable.ToArray();
            actor.EnqueueReceive(receiveSize);

            if (fabricHosts.Length == 0)
            {
                await caller.Send_SendRequestDone_ToApiAsync(
                    new SendRequestDoneDto(
                        request.Routing,
                        false,
                        $"No fabric connections found with userId {request.Routing.UserId} or sessionId {request.Routing.SessionId}"),
                    actor);
                return;
            }

            var state = new RequestState
            {
                Routing = request.Routing,
                Caller = caller,
                Actor = actor,
                Targets = fabricHosts
            };

            if (!OpenRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(TimeSpan.FromSeconds(100), () =>
            {
                state.Exceptions.TryAdd(state.Caller.FabricConnectionId, "Request timed out.");
                _ = CompleteRequestAsync(logger, state);
            });

            foreach (var fabricHost in fabricHosts)
                await fabricHost.Send_SendRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_SendRequestCancelled_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SendRequestCancelledDto cancel, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_SendRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, receiveSize);

            if (!OpenRequests.TryGetValue(cancel.Routing.RequestId, out var state))
                return;

            foreach (var target in state.Targets)
            {
                await target.Send_SendRequestCancelled_ToApiAsync(cancel, state.Actor);
            }
        }, ct);
    }
    public async Task Receive_SendRequestDone_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SendRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_SendRequestDone_FromApiAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), done, receiveSize);

            if (!OpenRequests.TryGetValue(done.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricConnectionId] = 1;
            if (done.ExceptionMessage != null)
            {
                state.Exceptions.TryAdd(caller.FabricConnectionId, done.ExceptionMessage);
            }
            if (done.Cancelled)
            {
                state.Cancel();
            }


            if (state.CompletedTargets.Count == state.Targets.Length)
            {
                await CompleteRequestAsync(logger, state);
            }
        }, ct);
    }
    private async Task CompleteRequestAsync(ILogger<FabricManager> logger, RequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: CompleteRequestAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptionMessage = state.Exceptions.Count == 0 ? null : string.Join(", ", state.Exceptions.Values);
        await state.Caller.Send_SendRequestDone_ToApiAsync(
            new SendRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage
            ), state.Actor);
    }

    public async Task Receive_InvokeRequest_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, InvokeRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_InvokeRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            (var fabricHostsEnumerable, var actor) = Services[request.Routing.ServiceId]
                .GetFabricHosts(request.Routing.UserId, request.Routing.SessionId);
            var fabricHosts = fabricHostsEnumerable.ToArray();
            actor.EnqueueReceive(receiveSize);

            if (fabricHosts.Length == 0)
            {
                await caller.Send_SendRequestDone_ToApiAsync(
                    new SendRequestDoneDto(
                        request.Routing,
                        false,
                        $"No fabric connections found with userId {request.Routing.UserId} or sessionId {request.Routing.SessionId}"),
                    actor);
                return;
            }

            var state = new RequestState
            {
                Routing = request.Routing,
                Actor = actor,
                Caller = caller,
                Targets = fabricHosts
            };

            if (!OpenRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(TimeSpan.FromSeconds(100), () =>
            {
                state.Exceptions.TryAdd(state.Caller.FabricConnectionId, "Invoke request timed out.");
                _ = ReadyInvokeAsync(logger, state);
            });

            foreach (var host in fabricHosts)
                await host.Send_InvokeRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_InvokeRequestCancelled_FromApiAsync(FabricHost fabricHost, ILogger<FabricManager> logger, InvokeRequestCancelledDto cancel, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_InvokeRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, receiveSize);

            if (!OpenRequests.TryGetValue(cancel.Routing.RequestId, out var state))
                return;

            foreach (var target in state.Targets)
            {
                await target.Send_InvokeRequestCancelled_ToApiAsync(cancel, state.Actor);
            }
        }, ct);
    }
    public async Task Receive_InvokeRequestDoneAsync(FabricHost caller, ILogger<FabricManager> logger, InvokeRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_InvokeRequestDoneAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), done, receiveSize);

            if (!OpenRequests.TryGetValue(done.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.ReadyTargets[caller.FabricConnectionId] = 0;
            if (done.ExceptionMessage != null)
            {
                state.Exceptions.TryAdd(caller.FabricConnectionId, done.ExceptionMessage);
            }
            if (done.Cancelled)
            {
                state.Cancel();
            }

            if (state.ReadyTargets.Count == state.Targets.Length)
            {
                await ReadyInvokeAsync(logger, state);
            }
        }, ct);
    }
    private async Task ReadyInvokeAsync(ILogger<FabricManager> logger, RequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;

        await state.Caller.Send_InvokeRequestDone_ToApiAsync(
            new InvokeRequestDoneDto(
                state.Routing,
                state.Cancelled,
                state.Exceptions.Count == 0 ? null : string.Join(", ", state.Exceptions.Values)
            ), state.Actor);
    }

    // De orginele iteratie op de InvokeRequest:
    public async Task Receive_StreamingRequestServerToClient_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, StreamingRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_StreamingRequestServerToClient_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            if (!OpenRequests.TryGetValue(request.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var target in state.Targets)
            {
                await target.Send_StreamingRequestServerToClient_ToApiAsync(request, state.Actor);
            }
        }, ct);
    }
    public async Task Receive_StreamingResponseClientToServer_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, StreamingResponseDto response, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_StreamingResponseClientToServer_FromApiAsync({response}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), response, receiveSize);

            if (!OpenRequests.TryGetValue(response.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (response.IsCompleted ||
                response.IsCancelled ||
                response.ExceptionMessage != null)
            {
                state.CompletedTargets.TryAdd(caller.FabricConnectionId, 0);

                if (response.ExceptionMessage != null)
                {
                    state.Exceptions.TryAdd(
                        caller.FabricConnectionId,
                        response.ExceptionMessage);
                }

                if (response.IsCancelled)
                {
                    state.Cancel();
                }

                if (state.CompletedTargets.Count == state.Targets.Length &&
                    state.TryComplete())
                {
                    OpenRequests.TryRemove(state.Routing.RequestId, out _);

                    response = new StreamingResponseDto(
                        response.Routing,
                        response.ArgumentIndex,
                        response.StreamId,
                        true,
                        state.Cancelled,
                        state.Exceptions.Count == 0
                            ? null
                            : string.Join(", ", state.Exceptions.Values),
                        response.BinaryData);

                    await state.Caller.Send_StreamingResponseClientToServer_ToApiAsync(
                        response,
                        state.Actor);
                }

                return;
            }

            response = new StreamingResponseDto(
                response.Routing,
                response.ArgumentIndex,
                response.StreamId,
                false,
                false,
                null,
                response.BinaryData);

            await state.Caller.Send_StreamingResponseClientToServer_ToApiAsync(
                response,
                state.Actor);
        }, ct);
    }

    // De argument iteraties die meegegeven zijn bij de SendRequest / InvokeRequest:
    public async Task Receive_StreamingRequestClientToServer_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, StreamingRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_StreamingRequestClientToServer_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            if (!OpenRequests.TryGetValue(request.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryAdd(request.StreamId, caller))
            {
                await state.Caller.Send_StreamingRequestClientToServer_ToApiAsync(request, state.Actor);
            }
        }, ct);
    }
    public async Task Receive_StreamingResponseServerToClient_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, StreamingResponseDto response, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_StreamingResponseServerToClient_FromApiAsync({response}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), response, receiveSize);

            if (!OpenRequests.TryGetValue(response.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryRemove(response.StreamId, out var sender))
            {
                await sender.Send_StreamingResponseServerToClient_ToApiAsync(response, state.Actor);
            }
        }, ct);
    }


    public async Task Receive_ServerSendRequest_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, ServerSendRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_ServerSendRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            var actor = Services[request.Routing.ServiceId];
            var fabricHosts = Connections
                .Where(conn => request.Routing.FabricConnectionId == conn.FabricConnectionId)
                .ToArray();
            actor.EnqueueReceive(receiveSize);
            if (fabricHosts.Length == 0)
            {
                await caller.Send_ServerSendRequestDone_ToApiAsync(
                    new ServerSendRequestDoneDto(
                        request.Routing,
                        false,
                        $"No fabric connections found with id {request.Routing.FabricConnectionId}"),
                    actor);
                return;
            }

            var state = new ServerRequestState
            {
                Routing = request.Routing,
                Caller = caller,
                Actor = actor,
                Targets = fabricHosts
            };

            if (!OpenServerRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(TimeSpan.FromSeconds(100), () =>
            {
                state.Exceptions.TryAdd(state.Caller.FabricConnectionId, "Request timed out.");
                _ = CompleteServerRequestAsync(logger, state);
            });

            foreach (var fabricHost in fabricHosts)
                await fabricHost.Send_ServerSendRequest_ToApiAsync(request, actor);
        }, ct);

    }
    public async Task Receive_ServerInvokeRequest_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, ServerInvokeRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_ServerInvokeRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            var actor = Services[request.Routing.ServiceId];
            var fabricHosts = Connections
                .Where(conn => request.Routing.FabricConnectionId == conn.FabricConnectionId)
                .ToArray();
            actor.EnqueueReceive(receiveSize);
            if (fabricHosts.Length == 0)
            {
                await caller.Send_ServerSendRequestDone_ToApiAsync(
                    new ServerSendRequestDoneDto(
                        request.Routing,
                        false,
                        $"No fabric connections found with id {request.Routing.FabricConnectionId}"),
                    actor);
                return;
            }

            var state = new ServerRequestState
            {
                Routing = request.Routing,
                Actor = actor,
                Caller = caller,
                Targets = fabricHosts
            };

            if (!OpenServerRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(TimeSpan.FromSeconds(100), () =>
            {
                state.Exceptions.TryAdd(state.Caller.FabricConnectionId, "Invoke request timed out.");
                _ = ReadyServerInvokeAsync(logger, state);
            });

            foreach (var host in fabricHosts)
                await host.Send_ServerInvokeRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_ServerStreamingRequest_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, ServerStreamingRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_StreamingRequestClientToServer_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            if (!OpenServerRequests.TryGetValue(request.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryAdd(request.StreamId, caller))
            {
                await state.Caller.Send_ServerStreamingRequestClientToServer_ToApiAsync(request, state.Actor);
            }
        }, ct);
    }

    private async Task CompleteServerRequestAsync(ILogger<FabricManager> logger, ServerRequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: CompleteRequestAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptionMessage = state.Exceptions.Count == 0 ? null : string.Join(", ", state.Exceptions.Values);
        await state.Caller.Send_ServerSendRequestDone_ToApiAsync(
            new ServerSendRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage
            ), state.Actor);
    }
    private async Task ReadyServerInvokeAsync(ILogger<FabricManager> logger, ServerRequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;

        await state.Caller.Send_ServerInvokeRequestDone_ToApiAsync(
            new ServerInvokeRequestDoneDto(
                state.Routing,
                state.Cancelled,
                state.Exceptions.Count == 0 ? null : string.Join(", ", state.Exceptions.Values)
            ), state.Actor);
    }



    public async Task DisconnectAllAsync()
    {
        foreach (var conn in Connections)
            conn.Dispose();

        //NotifyChanged();
    }

    public FabricDashboardSnapshot GetDashboardSnapshot()
    {
        var connections = Connections
            .Select(connection => {
                var send = connection.GetSendBytesPerSecond();
                var receive = connection.GetReceiveBytesPerSecond();
                return new FabricConnectionSnapshot(
                    connection.FabricConnectionId.Value,
                    send.bytes,
                    receive.bytes,
                    send.count,
                    receive.count);
            })
            .OrderBy(connection => connection.ConnectionId)
            .ToArray();

        var services = Services
            .Select(service =>
            {
                
                var sendSpeed = 0L;
                var sendCount = 0;

                var send = service.GetSendSpeed();
                sendSpeed += send.bytes;
                sendCount += send.count;
                foreach (var session in service.Sessions)
                {
                    var sendSession = session.GetSendSpeed();
                    sendSpeed += sendSession.bytes;
                    sendCount += sendSession.count;
                }
                foreach (var user in service.Users)
                {
                    var sendUser = user.GetSendSpeed();
                    sendSpeed += sendUser.bytes;
                    sendCount += sendUser.count;
                }


                var receivedSpeed = 0L;
                var receivedCount = 0;

                var received = service.GetReceiveSpeed();
                receivedSpeed += received.bytes;
                receivedCount += received.count;
                foreach (var session in service.Sessions)
                {
                    var receivedSession = session.GetReceiveSpeed();
                    receivedSpeed += receivedSession.bytes;
                    receivedCount += receivedSession.count;
                }
                foreach (var user in service.Users)
                {
                    var receivedUser = user.GetReceiveSpeed();
                    receivedSpeed += receivedUser.bytes;
                    receivedCount += receivedUser.count;
                }
                return new FabricServiceSnapshot(
                    service.Id.ToString(),
                    sendSpeed,
                    receivedSpeed,
                    sendCount,
                    receivedCount,
                    service.Sessions.Count,
                    service.Users.Count);
            })
            .OrderBy(service => service.ServiceId, StringComparer.Ordinal)
            .ToArray();

        return new FabricDashboardSnapshot(DateTimeOffset.UtcNow, Config.Port, connections, services);
    }


    public async Task DisposeAsync()
    {
        await DisconnectAllAsync();
    }

}
