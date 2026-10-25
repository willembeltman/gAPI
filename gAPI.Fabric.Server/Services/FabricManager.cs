using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Dashboard;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Config;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Services;

public class FabricManager : IDashboardSnapshotGetter
{
    private readonly SessionCache SessionCache; // Voor de client, niet voor hier :)

    public readonly FabricConfig Config;
    public readonly FabricManagerId FabricManagerId;
    public readonly FabricHostCollection AllConnections;
    public readonly FabricBusHostCollection AllBusConnections;
    public readonly ServiceCollection Services;
    public readonly ConcurrentDictionary<RequestId, RequestState> OpenRequests;
    public readonly ConcurrentDictionary<RequestId, BusRequestState> OpenBusRequests;
    private int _roundRobinIndex = 0;
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
        AllConnections = new();
        AllBusConnections = new();
        Services = new(this);
        OpenRequests = new();
        OpenBusRequests = new();
        Console = consoleBuffer; // Initial state

        System = Services[new ServiceId("Fabric System")];
        Logging = Services[new ServiceId("Fabric Logging")];
    }

    public void StartNewFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricHost = new FabricHost(this, tcpClient);
        fabricHost.Start();
    }
    public void StartNewBusFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricBusHost = new FabricBusHost(this, tcpClient);
        fabricBusHost.Start();
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
        }, ct);
    }

    public async Task Receive_SendAndForgetRequest_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SendRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_SendAndForgetRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            (var fabricHostsEnumerable, var actor) = Services[request.Routing.ServiceId]
                .GetFabricHosts(request.Routing.UserId, request.Routing.SessionId);
            var fabricHosts = fabricHostsEnumerable.ToArray();
            actor.EnqueueReceive(receiveSize);

            foreach (var fabricHost in fabricHosts)
                await fabricHost.Send_SendAndForgetRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_SendAndForgetRequestCancelled_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, SendRequestCancelledDto cancel, long receiveSize, CancellationToken ct)
    {
        // Fire and forget so we just search for everybody and fire cancel
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_SendAndForgetRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, receiveSize);

            (var fabricHostsEnumerable, var actor) = Services[cancel.Routing.ServiceId]
                .GetFabricHosts(cancel.Routing.UserId, cancel.Routing.SessionId);
            var fabricHosts = fabricHostsEnumerable.ToArray();
            actor.EnqueueReceive(receiveSize);

            foreach (var fabricHost in fabricHosts)
                await fabricHost.Send_SendAndForgetRequestCancelled_ToApiAsync(cancel, actor);
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

            state.StartTimeout(request.Routing.Timeout, () =>
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

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets)
                await host.Send_SendRequestCancelled_ToApiAsync(cancel, state.Actor);

            state.Cancel();
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

            state.StartTimeout(request.Routing.Timeout, () =>
            {
                state.Exceptions.TryAdd(state.Caller.FabricConnectionId, "Invoke request timed out.");
                _ = ReadyInvokeAsync(logger, state);
            });

            foreach (var host in fabricHosts)
                await host.Send_InvokeRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_InvokeRequestCancelled_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, InvokeRequestCancelledDto cancel, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_InvokeRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, receiveSize);

            if (!OpenRequests.TryGetValue(cancel.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets)
                await host.Send_InvokeRequestCancelled_ToApiAsync(cancel, state.Actor);

            state.Cancel();
        }, ct);
    }
    public async Task Receive_InvokeRequestAck_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, InvokeRequestAckDto done, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_InvokeRequestAckAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), done, receiveSize);

            if (!OpenRequests.TryGetValue(done.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            // TODO
            state.ReadyTargets[caller.FabricConnectionId] = 0;

            if (state.ReadyTargets.Count == state.Targets.Length)
            {
                await ReadyInvokeAsync(logger, state);
            }
        }, ct);
    }
    public async Task Receive_InvokeRequestDone_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, InvokeRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_InvokeRequestDone_FromApiAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), done, receiveSize);

            if (!OpenRequests.TryGetValue(done.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricConnectionId] = 0;
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
                await CompleteInvokeAsync(logger, state);
            }
        }, ct);
    }
    private async Task ReadyInvokeAsync(ILogger<FabricManager> logger, RequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;

        await state.Caller.Send_InvokeRequestAck_ToApiAsync(
            new InvokeRequestAckDto(
                state.Routing
            ), state.Actor);
    }
    private async Task CompleteInvokeAsync(ILogger<FabricManager> logger, RequestState state)
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

    public async Task Receive_StreamingRequest_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, StreamingRequestDto request, long receiveSize, CancellationToken ct)
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
                if (request.ArgumentIndex < 0)
                {
                    foreach (var target in state.Targets)
                    {
                        await target.Send_StreamingRequest_ToApiAsync(request, state.Actor);
                    }
                }
                else
                {
                    await state.Caller.Send_StreamingRequest_ToApiAsync(request, state.Actor);
                }
            }
        }, ct);
    }
    public async Task Receive_StreamingResponse_FromApiAsync(FabricHost caller, ILogger<FabricManager> logger, StreamingResponseDto response, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_StreamingResponse_FromApiAsync({response}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), response, receiveSize);

            if (!OpenRequests.TryGetValue(response.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryRemove(response.StreamId, out var sender))
            {
                await sender.Send_StreamingResponse_ToApiAsync(response, state.Actor);
            }
        }, ct);
    }

    public async Task Receive_BusStreamingRequest_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusStreamingRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_BusStreamingRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            if (!OpenBusRequests.TryGetValue(request.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryAdd(request.StreamId, caller))
            {
                if (request.ArgumentIndex < 0)
                {
                    foreach (var target in state.Targets)
                    {
                        await target.Send_BusStreamingRequest_ToApiAsync(request, state.Actor);
                    }
                }
                else
                {
                    await state.Caller.Send_BusStreamingRequest_ToApiAsync(request, state.Actor);
                }
            }
        }, ct);
    }
    public async Task Receive_BusStreamingResponse_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusStreamingResponseDto response, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_BusStreamingResponse_FromApiAsync({response}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), response, receiveSize);

            if (!OpenBusRequests.TryGetValue(response.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryRemove(response.StreamId, out var sender))
            {
                await sender.Send_BusStreamingResponse_ToApiAsync(response, state.Actor);
            }
        }, ct);
    }



    public async Task Receive_BusSubscribe_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusSubscribeDto subscribe, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_Subscribe_FromApiAsync({subscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe, receiveSize);

            await Services[subscribe.ServiceId]
                .BusSubscribe(caller, subscribe.BusConnectionId, receiveSize);
        }, ct);
    }
    public async Task Receive_BusUnsubscribe_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusUnsubscribeDto unsubscribe, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_Unsubscribe_FromApiAsync({unsubscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe, receiveSize);

            await Services[unsubscribe.ServiceId]
                .BusUnsubscribe(caller, unsubscribe.BusConnectionId, receiveSize);
        }, ct);
    }

    public async Task Receive_BusSendFireAndForget_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusSendRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            (var targets, var actor) = ResolveBusTargets(request.Routing, receiveSize);
            foreach (var target in targets)
            {
                await target.Send_BusFireAndForget_ToApiAsync(request, actor);
            }
        }, ct);
    }
    public async Task Receive_BusSendFireAndForgetCancelled_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusSendRequestCancelledDto cancel, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            (var targets, var actor) = ResolveBusTargets(cancel.Routing, receiveSize);
            foreach (var target in targets)
            {
                await target.Send_BusFireAndForgetCancelled_ToApiAsync(cancel, actor);
            }
        }, ct);
    }

    public async Task Receive_BusSendRequest_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusSendRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            (var fabricHosts, var actor) = ResolveBusTargets(request.Routing, receiveSize);

            if (fabricHosts.Length == 0)
            {
                var ack = new BusSendRequestDoneDto(request.Routing, false, $"No active service bus subscribers found for service '{request.Routing.ServiceId}'");
                await caller.Send_BusSendRequestDone_ToApiAsync(ack, actor);
                return;
            }

            var state = new BusRequestState
            {
                Routing = request.Routing,
                Actor = actor,
                Caller = caller,
                Targets = fabricHosts
            };

            if (!OpenBusRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(request.Routing.Timeout, () =>
            {
                state.Exceptions.TryAdd(state.Caller.FabricBusConnectionId, "Invoke request timed out.");
                _ = CompleteBusRequestAsync(logger, state);
            });

            foreach (var host in fabricHosts)
                await host.Send_BusSendRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_BusSendRequestCancelled_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusSendRequestCancelledDto cancel, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_SendRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, receiveSize);

            if (!OpenBusRequests.TryGetValue(cancel.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets)
                await host.Send_BusSendRequestCancelled_ToApiAsync(cancel, state.Actor);

            state.Cancel();
        }, ct);
    }
    public async Task Receive_BusSendRequestDone_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusSendRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_SendRequestDone_FromApiAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), done, receiveSize);

            if (!OpenBusRequests.TryGetValue(done.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricBusConnectionId] = 1;
            if (done.ExceptionMessage != null)
            {
                state.Exceptions.TryAdd(caller.FabricBusConnectionId, done.ExceptionMessage);
            }
            if (done.Cancelled)
            {
                state.Cancel();
            }

            if (state.CompletedTargets.Count == state.Targets.Length)
            {
                await CompleteBusRequestAsync(logger, state);
            }
        }, ct);
    }
    private async Task CompleteBusRequestAsync(ILogger<FabricManager> logger, BusRequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: CompleteRequestAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenBusRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptionMessage = state.Exceptions.Count == 0 ? null : string.Join(", ", state.Exceptions.Values);
        await state.Caller.Send_BusSendRequestDone_ToApiAsync(
            new BusSendRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage
            ), state.Actor);
    }

    public async Task Receive_BusInvokeRequest_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusInvokeRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_BusInvokeRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            (var fabricHosts, var actor) = ResolveBusTargets(request.Routing, receiveSize);

            if (fabricHosts.Length == 0)
            {
                await caller.Send_BusSendRequestDone_ToApiAsync(
                    new BusSendRequestDoneDto(
                        request.Routing,
                        false,
                        $"No fabric connections found with To {request.Routing.To} or ToBusConnectionId {request.Routing.ToBusConnectionId}"),
                    actor);
                return;
            }

            var state = new BusRequestState
            {
                Routing = request.Routing,
                Actor = actor,
                Caller = caller,
                Targets = fabricHosts
            };

            if (!OpenBusRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(request.Routing.Timeout, () =>
            {
                state.Exceptions.TryAdd(state.Caller.FabricBusConnectionId, "Invoke request timed out.");
                _ = ReadyBusInvokeAsync(logger, state);
            });

            foreach (var host in fabricHosts)
                await host.Send_BusInvokeRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_BusInvokeRequestCancelled_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusInvokeRequestCancelledDto cancel, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_BusInvokeRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, receiveSize);

            if (!OpenBusRequests.TryGetValue(cancel.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets)
                await host.Send_BusInvokeRequestCancelled_ToApiAsync(cancel, state.Actor);

            state.Cancel();
        }, ct);
    }
    public async Task Receive_BusInvokeRequestAck_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusInvokeRequestAckDto done, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_BusInvokeRequestAckAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), done, receiveSize);

            if (!OpenBusRequests.TryGetValue(done.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.ReadyTargets[caller.FabricBusConnectionId] = 0;

            if (state.ReadyTargets.Count == state.Targets.Length)
            {
                await ReadyBusInvokeAsync(logger, state);
            }
        }, ct);
    }
    public async Task Receive_BusInvokeRequestDone_FromApiAsync(FabricBusHost caller, ILogger<FabricManager> logger, BusInvokeRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("{now}: Receive_BusInvokeRequestDoneAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), done, receiveSize);

            if (!OpenBusRequests.TryGetValue(done.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricBusConnectionId] = 0;
            if (done.ExceptionMessage != null)
            {
                state.Exceptions.TryAdd(caller.FabricBusConnectionId, done.ExceptionMessage);
            }
            if (done.Cancelled)
            {
                state.Cancel();
            }

            if (state.CompletedTargets.Count == state.Targets.Length)
            {
                await CompleteBusInvokeAsync(logger, state);
            }
        }, ct);
    }
    private async Task ReadyBusInvokeAsync(ILogger<FabricManager> logger, BusRequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;

        await state.Caller.Send_BusInvokeRequestAck_ToApiAsync(
            new BusInvokeRequestAckDto(
                state.Routing
            ), state.Actor);
    }
    private async Task CompleteBusInvokeAsync(ILogger<FabricManager> logger, BusRequestState state)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;

        await state.Caller.Send_BusInvokeRequestDone_ToApiAsync(
            new BusInvokeRequestDoneDto(
                state.Routing,
                state.Cancelled,
                state.Exceptions.Count == 0 ? null : string.Join(", ", state.Exceptions.Values)
            ), state.Actor);
    }



    private (FabricBusHost[], IActor) ResolveBusTargets(BusRoutingDto routing, long receiveSize)
    {
        var service = Services[routing.ServiceId];
        service.EnqueueReceive(receiveSize);

        if (routing.To == BusToChoice.ToBusConnectionId && routing.ToBusConnectionId != null)
        {
            var target = service.BusConnections.TryGet(routing.ToBusConnectionId);
            if (target == null)
                return ([], service);
            return ([target.Connection], target);
        }
        var subscribers = service.BusConnections.Select(a => a.Connection);
        var hosts = subscribers.Distinct().ToArray();
        if (hosts.Length == 0)
            return ([], service);

        if (routing.To == BusToChoice.ToAll)
        {
            return (hosts, service);
        }

        // ToAny => Round-robin
        var idx = (uint)Interlocked.Increment(ref _roundRobinIndex) % (uint)hosts.Length;
        return ([hosts[idx]], service);
    }

    public void Disconnected(FabricHost host)
    {
        var connectionId = host.FabricConnectionId;

        AllConnections.RemoveConnection(connectionId);
        Services.RemoveConnection(host);

        // Verwijder efficiënt zonder handmatige tussenlijsten
        foreach (var item in OpenRequests)
        {
            if (item.Value.Caller.FabricConnectionId == connectionId)
            {
                OpenRequests.TryRemove(item);
            }
        }
    }
    public void Disconnected(FabricBusHost host)
    {
        var connectionId = host.FabricBusConnectionId;

        AllBusConnections.RemoveConnection(connectionId);
        Services.RemoveBusConnection(host);

        foreach (var item in OpenBusRequests)
        {
            if (item.Value.Caller.FabricBusConnectionId == connectionId)
            {
                OpenBusRequests.TryRemove(item);
            }
        }
    }
    public async Task DisconnectAllAsync()
    {
        foreach (var conn in AllConnections)
            conn.Dispose();

        //NotifyChanged();
    }

    public FabricDashboardSnapshot GetDashboardSnapshot()
    {
        var connections1 = AllConnections
            .Select(connection =>
            {
                var send = connection.GetSendBytesPerSecond();
                var receive = connection.GetReceiveBytesPerSecond();
                return new FabricConnectionSnapshot(
                    "#" + connection.FabricConnectionId.Value.ToString(),
                    send.bytes,
                    receive.bytes,
                    send.count,
                    receive.count);
            });
        var connections2 = AllBusConnections
            .Select(connection =>
            {
                var send = connection.GetSendBytesPerSecond();
                var receive = connection.GetReceiveBytesPerSecond();
                return new FabricConnectionSnapshot(
                    "Bus #" + connection.FabricBusConnectionId.Value.ToString(),
                    send.bytes,
                    receive.bytes,
                    send.count,
                    receive.count);
            });

        var connections = connections1
            .Concat(connections2)
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
                foreach (var busconnection in service.BusConnections)
                {
                    var sendUser = busconnection.GetSendSpeed();
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

        return new FabricDashboardSnapshot(DateTimeOffset.UtcNow, connections, services);
    }


    public async Task DisposeAsync()
    {
        await DisconnectAllAsync();
    }
}
