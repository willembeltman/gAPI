using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Dashboard;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Fabric;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Config;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Services;

public class FabricManager : IDashboardSnapshotGetter
{
    private readonly SessionCache SessionCache; // Voor de client, niet voor hier :)

    public readonly FabricConfig Config;
    public readonly FabricManagerId FabricManagerId;

    public ILoggerFactory LoggerFactory { get; }

    public readonly FabricHostCollection AllConnections;
    public readonly FabricBusHostCollection AllBusConnections;
    public readonly FabricPlaneHostCollection AllPlaneConnections;
    public readonly ServiceCollection Services;

    public readonly ConcurrentDictionary<RequestId, SendRequestState> OpenSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, BusSendRequestState> OpenBusSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, PlaneSendRequestState> OpenPlaneSendRequests = [];

    public readonly ConcurrentDictionary<RequestId, InvokeRequestState> OpenInvokeRequests = [];
    public readonly ConcurrentDictionary<RequestId, BusInvokeRequestState> OpenBusInvokeRequests = [];
    public readonly ConcurrentDictionary<RequestId, PlaneInvokeRequestState> OpenPlaneInvokeRequests = [];

    private readonly ILogger<FabricManager> Logger;
    private int _roundRobinIndex = 0;
    public readonly Service Logging;
    public readonly Service System;


    public FabricManager(
        ILoggerFactory loggerFactory,
        FabricConfig config)
    {
        FabricManagerId = FabricManagerId.New();

        LoggerFactory = loggerFactory;
        Config = config;

        SessionCache = new();
        AllConnections = new();
        AllBusConnections = new();
        AllPlaneConnections = new();
        Services = new(this);
        Logger = loggerFactory.CreateLogger<FabricManager>();

        System = Services[new ServiceId("Fabric System")];
        Logging = Services[new ServiceId("Fabric Logging")];
    }

    public void StartNewFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricHost = new FabricHost(LoggerFactory, this, tcpClient);
        fabricHost.Start();
    }
    public void Disconnected(FabricHost host)
    {
        var connectionId = host.FabricConnectionId;

        AllConnections.RemoveConnection(connectionId);
        Services.RemoveConnection(host);

        // Verwijder efficiënt zonder handmatige tussenlijsten
        foreach (var item in OpenSendRequests)
        {
            if (item.Value.Caller.FabricConnectionId == connectionId)
            {
                OpenSendRequests.TryRemove(item);
            }
            item.Value.RemoveHost(host);
        }
    }

    public void StartNewBusFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricBusHost = new FabricBusHost(LoggerFactory, this, tcpClient);
        fabricBusHost.Start();
    }
    public void Disconnected(FabricBusHost host)
    {
        var connectionId = host.FabricBusConnectionId;

        AllBusConnections.RemoveConnection(connectionId);
        Services.RemoveBusConnection(host);

        foreach (var item in OpenBusSendRequests)
        {
            if (item.Value.Caller.FabricBusConnectionId == connectionId)
            {
                OpenBusSendRequests.TryRemove(item);
            }
            item.Value.RemoveBusHost(host);
        }
    }

    public void StartNewPlaneFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricPlaneHost = new FabricPlaneHost(LoggerFactory, this, tcpClient);
        fabricPlaneHost.Start();
    }
    public void Disconnected(FabricPlaneHost host)
    {
        var connectionId = host.FabricPlaneConnectionId;

        AllPlaneConnections.RemoveConnection(connectionId);
        Services.RemovePlaneConnection(host);

        foreach (var item in OpenPlaneSendRequests)
        {
            if (item.Value.Caller.FabricPlaneConnectionId == connectionId)
            {
                OpenPlaneSendRequests.TryRemove(item);
            }
            item.Value.RemovePlaneHost(host);
        }
    }
    public async Task DisconnectAllAsync()
    {
        foreach (var conn in AllConnections)
            conn.Dispose();
    }

    #region Normal Wss/Api fabric (9494)

    public async Task Receive_UpdateSession_FromApiAsync(FabricHost caller, UpdateSessionDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_UpdateSession_FromApiAsync({updateSession}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);
            SessionCache.AddOrUpdate(message.SessionId, message.CookieData);
        }, ct);
    }
    public async Task Receive_ClearSession_FromApiAsync(FabricHost caller, SendClearSessionDto message, long receiveSize, CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_ClearSession_FromApiAsync({clearSession}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);
            SessionCache.Remove(message.SessionId);
        }, token);
    }
    public async Task Receive_GetSessionCookieData_FromApiAsync(FabricHost caller, SendGetSessionCookieDataDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_GetSessionCookieData_FromApiAsync({getSessionCookieData}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);
            var sessionId = message.SessionId;
            string? cookieData = null;
            SessionCache.TryGet(sessionId, out cookieData);
            var getSessionCookieDataResponse = new SendGetSessionCookieDataResponseDto(sessionId, cookieData);
            await caller.Send_GetSessionCookieDataResponse_ToApiAsync(getSessionCookieDataResponse, System);
        }, ct);
    }

    public async Task Receive_Subscribe_FromApiAsync(FabricHost caller, SubscribeDto message, long receiveSize, CancellationToken ct)
    {
        //_ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_Subscribe_FromApiAsync({subscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            var actor = Services[message.ServiceId];
            await actor
                .Subscribe(caller, message.UserId, message.SessionId, receiveSize);

            var ack = new SubscribeAckDto(
                message,
                false,
                null);
            await caller.Send_SubscribeAck_ToApiAsync(ack, actor);
        }//, ct);
    }
    public async Task Receive_Unsubscribe_FromApiAsync(FabricHost caller, UnsubscribeDto message, long receiveSize, CancellationToken ct)
    {
        //_ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_Unsubscribe_FromApiAsync({unsubscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            var actor = Services[message.ServiceId];
            await actor
                .Unsubscribe(caller, message.UserId, message.SessionId, receiveSize);

            var ack = new UnsubscribeAckDto(
                message,
                false,
                null);
            await caller.Send_UnsubscribeAck_ToApiAsync(ack, actor);
        }//, ct);
    }

    public async Task Receive_SendAndForgetRequest_FromApiAsync(FabricHost caller, SendRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendAndForgetRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            (var fabricHostsEnumerable, var actor) = Services[message.Routing.ServiceId]
                .GetFabricHosts(message.Routing.UserId, message.Routing.SessionId);
            var fabricHosts = fabricHostsEnumerable.ToArray();
            actor.EnqueueReceive(receiveSize);

            foreach (var fabricHost in fabricHosts)
            {
                await fabricHost.Send_SendAndForgetRequest_ToApiAsync(message, actor);
            }
        }, ct);
    }

    public async Task Receive_SendRequest_FromApiAsync(FabricHost caller, SendRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            (var fabricHostsEnumerable, var actor) = Services[message.Routing.ServiceId]
                .GetFabricHosts(message.Routing.UserId, message.Routing.SessionId);
            var fabricHosts = new ConcurrentDictionary<FabricConnectionId, FabricHost>(
                fabricHostsEnumerable.ToDictionary(a => a.FabricConnectionId));
            actor.EnqueueReceive(receiveSize);

            if (fabricHosts.Count == 0)
            {
                if (message.Routing.ThrowIfNoReceiver)
                    await caller.Send_SendRequestDone_ToApiAsync(
                        new SendRequestDoneDto(
                            message.Routing,
                            false,
                            new ExceptionDto($"No fabric connections found with userId {message.Routing.UserId} or sessionId {message.Routing.SessionId}", null, Environment.StackTrace)),
                        actor);
                else
                    await caller.Send_SendRequestDone_ToApiAsync(
                        new SendRequestDoneDto(
                            message.Routing,
                            false,
                            null),
                        actor);
                return;
            }

            var state = new SendRequestState
            {
                Routing = message.Routing,
                Caller = caller,
                Actor = actor,
                Targets = fabricHosts
            };

            if (!OpenSendRequests.TryAdd(message.Routing.RequestId, state))
                return;

            state.StartTimeout(message.Routing.Timeout, async () =>
            {
                state.CompletedTargets[state.Caller.FabricConnectionId] = new SendRequestDoneDto(message.Routing, false, new ExceptionDto("Request timed out.", null, Environment.StackTrace));
                await CompleteRequestAsync(state);
            });

            foreach (var fabricHost in fabricHosts.Values)
                await fabricHost.Send_SendRequest_ToApiAsync(message, actor);
        }, ct);
    }
    public async Task Receive_SendRequestCancelled_FromApiAsync(FabricHost caller, SendRequestCancelledDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenSendRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets.Values)
                await host.Send_SendRequestCancelled_ToApiAsync(message, state.Actor);

            state.Cancel();

            await CompleteRequestAsync(state);
        }, ct);
    }
    public async Task Receive_SendRequestDone_FromApiAsync(FabricHost caller, SendRequestDoneDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendRequestDone_FromApiAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenSendRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricConnectionId] = message;

            if (state.CompletedTargets.Count == state.Targets.Count)
            {
                await CompleteRequestAsync(state);
            }
        }, ct);
    }
    private async Task CompleteRequestAsync(SendRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: CompleteRequestAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenSendRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptions = state.CompletedTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_SendRequestDone_ToApiAsync(
            new SendRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage == null
                ? null
                : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }

    public async Task Receive_InvokeRequest_FromApiAsync(FabricHost caller, InvokeRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_InvokeRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            (var fabricHostsEnumerable, var actor) = Services[message.Routing.ServiceId]
                .GetFabricHosts(message.Routing.UserId, message.Routing.SessionId);
            var fabricHosts = new ConcurrentDictionary<FabricConnectionId, FabricHost>(
                fabricHostsEnumerable.ToDictionary(a => a.FabricConnectionId));
            actor.EnqueueReceive(receiveSize);

            if (fabricHosts.Count == 0)
            {
                if (message.Routing.ThrowIfNoReceiver)
                    await caller.Send_InvokeRequestAck_ToApiAsync(
                        new InvokeRequestAckDto(
                            message.Routing,
                            false,
                            false,
                            new ExceptionDto($"No fabric connections found with userId {message.Routing.UserId} or sessionId {message.Routing.SessionId}, service: {message.Routing.ServiceId} method: {message.Routing.MethodId}", null, Environment.StackTrace)),
                        actor);
                else
                    await caller.Send_InvokeRequestAck_ToApiAsync(
                        new InvokeRequestAckDto(
                            message.Routing,
                            false,
                            false,
                            null),
                        actor);
                return;
            }

            var state = new InvokeRequestState
            {
                Routing = message.Routing,
                Actor = actor,
                Caller = caller,
                Targets = fabricHosts
            };

            if (!OpenInvokeRequests.TryAdd(message.Routing.RequestId, state))
                return;

            state.StartTimeout(message.Routing.Timeout, async () =>
            {
                state.ReadyTargets[state.Caller.FabricConnectionId] = new InvokeRequestAckDto(message.Routing, false, false, new ExceptionDto("Invoke request timed out.", null, null));
                await ReadyInvokeAsync(state);
                state.CompletedTargets[state.Caller.FabricConnectionId] = new InvokeRequestDoneDto(message.Routing, false, new ExceptionDto("Invoke request timed out.", null, null));
                await ReadyInvokeAsync(state);
            });

            foreach (var host in fabricHosts)
                await host.Value.Send_InvokeRequest_ToApiAsync(message, actor);
        }, ct);
    }
    public async Task Receive_InvokeRequestCancelled_FromApiAsync(FabricHost caller, InvokeRequestCancelledDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_InvokeRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets.Values)
                await host.Send_InvokeRequestCancelled_ToApiAsync(message, state.Actor);

            state.Cancel();
            await ReadyInvokeAsync(state);
            await CompleteInvokeAsync(state);
        }, ct);
    }
    public async Task Receive_InvokeRequestAck_FromApiAsync(FabricHost caller, InvokeRequestAckDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_InvokeRequestAckAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.ReadyTargets[caller.FabricConnectionId] = message;

            if (state.ReadyTargets.Count == state.Targets.Count)
            {
                await ReadyInvokeAsync(state);
            }
        }, ct);
    }
    public async Task Receive_InvokeRequestDone_FromApiAsync(FabricHost caller, InvokeRequestDoneDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_InvokeRequestDone_FromApiAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricConnectionId] = message;

            if (state.CompletedTargets.Count == state.Targets.Count)
            {
                await CompleteInvokeAsync(state);
            }
        }, ct);
    }
    private async Task ReadyInvokeAsync(InvokeRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;

        if (state.ReadyTargets.Values.All(a => a.EnumeratorCreated == false))
        {
            if (state.Routing.ThrowIfNoReceiver)
                await state.Caller.Send_InvokeRequestAck_ToApiAsync(
                    new InvokeRequestAckDto(
                        state.Routing,
                        false,
                        false,
                        new ExceptionDto($"No fabric connections found with userId {state.Routing.UserId} or sessionId {state.Routing.SessionId}, service: {state.Routing.ServiceId} method: {state.Routing.MethodId}", null, Environment.StackTrace)),
                    state.Actor);
            else
                await state.Caller.Send_InvokeRequestAck_ToApiAsync(
                    new InvokeRequestAckDto(
                        state.Routing,
                        false,
                        false,
                        null),
                    state.Actor);
            return;
        }

        var exceptions = state.ReadyTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_InvokeRequestAck_ToApiAsync(
            new InvokeRequestAckDto(
                state.Routing,
                true,
                false,
                exceptionMessage == null
                ? null
                : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }
    private async Task CompleteInvokeAsync(InvokeRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenInvokeRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptions = state.CompletedTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_InvokeRequestDone_ToApiAsync(
            new InvokeRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage == null
                ? null
                : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }

    public async Task Receive_StreamingRequest_FromApiAsync(FabricHost caller, StreamingRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_StreamingRequestClientToServer_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (OpenInvokeRequests.TryGetValue(message.Routing.RequestId, out var state2))
            {
                state2.ResetTimeout();
                state2.Actor.EnqueueReceive(receiveSize);

                if (state2.StreamRoutes.TryAdd(message.StreamId, caller))
                {
                    if (message.ArgumentIndex < 0)
                    {
                        foreach (var target in state2.Targets.Values)
                        {
                            await target.Send_StreamingRequest_ToApiAsync(message, state2.Actor);
                        }
                    }
                    else
                    {
                        await state2.Caller.Send_StreamingRequest_ToApiAsync(message, state2.Actor);
                    }
                }
            }
            if (OpenSendRequests.TryGetValue(message.Routing.RequestId, out var state))
            {
                state.ResetTimeout();
                state.Actor.EnqueueReceive(receiveSize);

                if (state.StreamRoutes.TryAdd(message.StreamId, caller))
                {
                    if (message.ArgumentIndex < 0)
                    {
                        foreach (var target in state.Targets.Values)
                        {
                            await target.Send_StreamingRequest_ToApiAsync(message, state.Actor);
                        }
                    }
                    else
                    {
                        await state.Caller.Send_StreamingRequest_ToApiAsync(message, state.Actor);
                    }
                }

            }

        }, ct);
    }
    public async Task Receive_StreamingResponse_FromApiAsync(FabricHost caller, StreamingResponseDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_StreamingResponse_FromApiAsync({response}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (OpenSendRequests.TryGetValue(message.Routing.RequestId, out var state))
            {
                state.ResetTimeout();
                state.Actor.EnqueueReceive(receiveSize);

                if (state.StreamRoutes.TryRemove(message.StreamId, out var sender))
                {
                    await sender.Send_StreamingResponse_ToApiAsync(message, state.Actor);
                }
            }

            if (OpenInvokeRequests.TryGetValue(message.Routing.RequestId, out var state2))
            {
                state2.ResetTimeout();
                state2.Actor.EnqueueReceive(receiveSize);

                if (state2.StreamRoutes.TryRemove(message.StreamId, out var sender))
                {
                    await sender.Send_StreamingResponse_ToApiAsync(message, state2.Actor);
                }
            }

        }, ct);
    }

    #endregion

    #region Bus

    public async Task Receive_BusSubscribe_FromApiAsync(FabricBusHost caller, BusSubscribeDto message, long receiveSize, CancellationToken ct)
    {
        //_ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_Subscribe_FromApiAsync({subscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            var actor = Services[message.ServiceId];
            await actor
                .BusSubscribe(caller, message.BusConnectionId, receiveSize);

            var ack = new BusSubscribeAckDto(
                message,
                false,
                null);
            await caller.Send_BusSubscribeAck_ToApiAsync(ack, actor);
        }//, ct);
    }
    public async Task Receive_BusUnsubscribe_FromApiAsync(FabricBusHost caller, BusUnsubscribeDto message, long receiveSize, CancellationToken ct)
    {
        //_ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_Unsubscribe_FromApiAsync({unsubscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            var actor = Services[message.ServiceId];
            await actor
                .BusUnsubscribe(caller, message.BusConnectionId, receiveSize);

            var ack = new BusUnsubscribeAckDto(
                message,
                false,
                null);
            await caller.Send_BusUnsubscribeAck_ToApiAsync(ack, actor);
        }//, ct);
    }

    public async Task Receive_BusSendFireAndForget_FromApiAsync(FabricBusHost caller, BusSendRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            (var targets, var actor) = ResolveBusTargets(message.Routing, receiveSize);
            foreach (var target in targets)
            {
                await target.Send_BusFireAndForget_ToApiAsync(message, actor);
            }
        }, ct);
    }

    public async Task Receive_BusSendRequest_FromApiAsync(FabricBusHost caller, BusSendRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            (var fabricHosts, var actor) = ResolveBusTargets(message.Routing, receiveSize);

            if (fabricHosts.Length == 0)
            {
                var ack = new BusSendRequestDoneDto(
                    message.Routing,
                    false,
                    new ExceptionDto($"No active service bus subscribers found for service '{message.Routing.ServiceId}', service: {message.Routing.ServiceId} method: {message.Routing.MethodId}", null, Environment.StackTrace));
                await caller.Send_BusSendRequestDone_ToApiAsync(ack, actor);
                return;
            }

            var state = new BusSendRequestState
            {
                Routing = message.Routing,
                Actor = actor,
                Caller = caller,
                Targets = new ConcurrentDictionary<FabricBusConnectionId, FabricBusHost>(
                    fabricHosts.ToDictionary(a => a.FabricBusConnectionId))
            };

            if (!OpenBusSendRequests.TryAdd(message.Routing.RequestId, state))
                return;

            state.StartTimeout(message.Routing.Timeout, async () =>
            {
                state.CompletedTargets[state.Caller.FabricBusConnectionId] = new BusSendRequestDoneDto(message.Routing, false, new ExceptionDto("Invoke request timed out.", null, Environment.StackTrace));
                await CompleteBusRequestAsync(state);
            });

            foreach (var host in fabricHosts)
                await host.Send_BusSendRequest_ToApiAsync(message, actor);
        }, ct);
    }
    public async Task Receive_BusSendRequestCancelled_FromApiAsync(FabricBusHost caller, BusSendRequestCancelledDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenBusSendRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets.Values)
                await host.Send_BusSendRequestCancelled_ToApiAsync(message, state.Actor);

            state.Cancel();
        }, ct);
    }
    public async Task Receive_BusSendRequestDone_FromApiAsync(FabricBusHost caller, BusSendRequestDoneDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendRequestDone_FromApiAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenBusSendRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricBusConnectionId] = message;

            if (state.CompletedTargets.Count == state.Targets.Count)
            {
                await CompleteBusRequestAsync(state);
            }
        }, ct);
    }
    private async Task CompleteBusRequestAsync(BusSendRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: CompleteRequestAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenBusSendRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptions = state.CompletedTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));


        await state.Caller.Send_BusSendRequestDone_ToApiAsync(
            new BusSendRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage == null
                ? null
                : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }

    public async Task Receive_BusInvokeRequest_FromApiAsync(FabricBusHost caller, BusInvokeRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_BusInvokeRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            (var fabricHosts, var actor) = ResolveBusTargets(message.Routing, receiveSize);

            if (fabricHosts.Length == 0)
            {
                if (message.Routing.ThrowIfNoReceiver)
                    await caller.Send_BusInvokeRequestAck_ToApiAsync(
                        new BusInvokeRequestAckDto(
                            message.Routing,
                            false,
                            false,
                            new ExceptionDto($"No fabric connections found with To {message.Routing.To} or ToBusConnection {message.Routing.ToBusConnectionId}, service: {message.Routing.ServiceId} method: {message.Routing.MethodId}", null, Environment.StackTrace)),
                            actor);
                else
                    await caller.Send_BusInvokeRequestAck_ToApiAsync(
                        new BusInvokeRequestAckDto(
                            message.Routing,
                            false,
                            false,
                            null),
                        actor);
                return;
            }

            var state = new BusInvokeRequestState
            {
                Routing = message.Routing,
                Actor = actor,
                Caller = caller,
                Targets = new ConcurrentDictionary<FabricBusConnectionId, FabricBusHost>(
                    fabricHosts.ToDictionary(a => a.FabricBusConnectionId))
            };

            if (!OpenBusInvokeRequests.TryAdd(message.Routing.RequestId, state))
                return;

            state.StartTimeout(message.Routing.Timeout, async () =>
            {
                state.ReadyTargets[state.Caller.FabricBusConnectionId] = new BusInvokeRequestAckDto(message.Routing, false, false, new ExceptionDto("Invoke request timed out.", null, null));
                await ReadyBusInvokeAsync(state);
            });

            foreach (var host in fabricHosts)
                await host.Send_BusInvokeRequest_ToApiAsync(message, actor);
        }, ct);
    }
    public async Task Receive_BusInvokeRequestCancelled_FromApiAsync(FabricBusHost caller, BusInvokeRequestCancelledDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_BusInvokeRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenBusInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets.Values)
                await host.Send_BusInvokeRequestCancelled_ToApiAsync(message, state.Actor);

            state.Cancel();

            await CompleteBusInvokeAsync(state);
        }, ct);
    }
    public async Task Receive_BusInvokeRequestAck_FromApiAsync(FabricBusHost caller, BusInvokeRequestAckDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_BusInvokeRequestAckAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenBusInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.ReadyTargets[caller.FabricBusConnectionId] = message;

            if (state.ReadyTargets.Count == state.Targets.Count)
            {
                await ReadyBusInvokeAsync(state);
            }
        }, ct);
    }
    public async Task Receive_BusInvokeRequestDone_FromApiAsync(FabricBusHost caller, BusInvokeRequestDoneDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_BusInvokeRequestDoneAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenBusInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricBusConnectionId] = message;

            if (state.CompletedTargets.Count == state.Targets.Count)
            {
                await CompleteBusInvokeAsync(state);
            }
        }, ct);
    }
    private async Task ReadyBusInvokeAsync(BusInvokeRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;

        if (state.ReadyTargets.Values.All(a => a.EnumeratorCreated == false))
        {
            if (state.Routing.ThrowIfNoReceiver)
                await state.Caller.Send_BusInvokeRequestAck_ToApiAsync(
                    new BusInvokeRequestAckDto(
                        state.Routing,
                        false,
                        false,
                        new ExceptionDto($"No fabric connections found with userId {state.Routing} or sessionId {state.Routing.ToBusConnectionId}, service: {state.Routing.ServiceId} method: {state.Routing.MethodId}", null, Environment.StackTrace)),
                    state.Actor);
            else
                await state.Caller.Send_BusInvokeRequestAck_ToApiAsync(
                    new BusInvokeRequestAckDto(
                        state.Routing,
                        false,
                        false,
                        null),
                    state.Actor);
            return;
        }

        var exceptions = state.ReadyTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_BusInvokeRequestAck_ToApiAsync(
            new BusInvokeRequestAckDto(
                state.Routing,
                true,
                false,
                null
            ), state.Actor);
    }
    private async Task CompleteBusInvokeAsync(BusInvokeRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        var exceptions = state.CompletedTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_BusInvokeRequestDone_ToApiAsync(
            new BusInvokeRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage == null
                    ? null
                    : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }

    public async Task Receive_BusStreamingRequest_FromApiAsync(FabricBusHost caller, BusStreamingRequestDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_BusStreamingRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenBusInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.Targets.Count == 0)
            {
                try
                {
                    await state.Caller.Send_BusStreamingResponse_ToApiAsync(
                        new BusStreamingResponseDto(
                            message.Routing,
                            message.ArgumentIndex,
                            message.StreamId,
                            true,
                            message.Cancelled,
                            message.Cancelled ? new ExceptionDto("Task is cancelled", null, Environment.StackTrace) : null,
                            []),
                        state.Actor);
                }
                finally
                {
                    OpenBusInvokeRequests.TryRemove(message.Routing.RequestId, out _);
                    state.TryComplete();
                }

                return;
            }

            if (state.StreamRoutes.TryAdd(message.StreamId, caller))
            {
                if (message.ArgumentIndex < 0)
                {
                    foreach (var target in state.Targets.Values)
                    {
                        await target.Send_BusStreamingRequest_ToApiAsync(message, state.Actor);
                    }
                }
                else
                {
                    await state.Caller.Send_BusStreamingRequest_ToApiAsync(message, state.Actor);
                }
            }
        }, ct);
    }
    public async Task Receive_BusStreamingResponse_FromApiAsync(FabricBusHost caller, BusStreamingResponseDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_BusStreamingResponse_FromApiAsync({response}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenBusInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryRemove(message.StreamId, out var sender))
            {
                await sender.Send_BusStreamingResponse_ToApiAsync(message, state.Actor);
            }
        }, ct);
    }

    private (FabricBusHost[], IActor) ResolveBusTargets(BusRoutingDto routing, long receiveSize)
    {
        var service = Services[routing.ServiceId];
        service.EnqueueReceive(receiveSize);

        if (routing.To == BusToChoice.ToBusConnection && routing.ToBusConnectionId != null)
        {
            if (service.BusConnections.TryGetValue(routing.ToBusConnectionId, out var target) == false)
                return ([], service);
            return ([target.Connection], target);
        }
        var subscribers = service.BusConnections.Values.Select(a => a.Connection);
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


    #endregion

    #region Plane

    public async Task Receive_PlaneSubscribe_FromApiAsync(FabricPlaneHost caller, PlaneSubscribeDto subscribe, long receiveSize, CancellationToken ct)
    {
        //_ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_Subscribe_FromApiAsync({subscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe, receiveSize);

            var actor = Services[subscribe.ServiceId];
            await actor
                .PlaneSubscribe(caller, subscribe.PlaneConnectionId, subscribe.ServiceSubscriptionId, receiveSize);

            var ack = new PlaneSubscribeAckDto(
                subscribe,
                false,
                null);
            await caller.Send_PlaneSubscribeAck_ToApiAsync(ack, actor);
        }//, ct);
    }
    public async Task Receive_PlaneUnsubscribe_FromApiAsync(FabricPlaneHost caller, PlaneUnsubscribeDto unsubscribe, long receiveSize, CancellationToken ct)
    {
        //_ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_Unsubscribe_FromApiAsync({unsubscribe}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe, receiveSize);

            var actor = Services[unsubscribe.ServiceId];
            await actor
                .PlaneUnsubscribe(caller, unsubscribe.PlaneConnectionId, unsubscribe.ServiceSubscriptionId, receiveSize);

            var ack = new PlaneUnsubscribeAckDto(
                unsubscribe,
                false,
                null);
            await caller.Send_PlaneUnsubscribeAck_ToApiAsync(ack, actor);
        }//, ct);
    }

    public async Task Receive_PlaneSendFireAndForget_FromApiAsync(FabricPlaneHost caller, PlaneSendRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            (var targets, var actor) = ResolvePlaneTargets(request.Routing, receiveSize);
            foreach (var target in targets)
            {
                await target.Send_PlaneFireAndForget_ToApiAsync(request, actor);
            }
        }, ct);
    }

    public async Task Receive_PlaneSendRequest_FromApiAsync(FabricPlaneHost caller, PlaneSendRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            (var fabricHosts, var actor) = ResolvePlaneTargets(request.Routing, receiveSize);

            if (fabricHosts.Length == 0)
            {
                var ack = new PlaneSendRequestDoneDto(
                    request.Routing,
                    false,
                    new ExceptionDto($"No active service bus subscribers found for service '{request.Routing.ServiceId}', service: {request.Routing.ServiceId} method: {request.Routing.MethodId}", null, Environment.StackTrace));
                await caller.Send_PlaneSendRequestDone_ToApiAsync(ack, actor);
                return;
            }

            var state = new PlaneSendRequestState
            {
                Routing = request.Routing,
                Actor = actor,
                Caller = caller,
                Targets = new ConcurrentDictionary<FabricPlaneConnectionId, FabricPlaneHost>(
                    fabricHosts.ToDictionary(a => a.FabricPlaneConnectionId))
            };

            if (!OpenPlaneSendRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(request.Routing.Timeout, () =>
            {
                state.CompletedTargets[state.Caller.FabricPlaneConnectionId] = new PlaneSendRequestDoneDto(request.Routing, false, new ExceptionDto("Invoke request timed out.", null, Environment.StackTrace));
                _ = CompletePlaneRequestAsync(state);
            });

            foreach (var host in fabricHosts)
                await host.Send_PlaneSendRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_PlaneSendRequestCancelled_FromApiAsync(FabricPlaneHost caller, PlaneSendRequestCancelledDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenPlaneSendRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets.Values)
                await host.Send_PlaneSendRequestCancelled_ToApiAsync(message, state.Actor);

            state.Cancel();
        }, ct);
    }
    public async Task Receive_PlaneSendRequestDone_FromApiAsync(FabricPlaneHost caller, PlaneSendRequestDoneDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_SendRequestDone_FromApiAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenPlaneSendRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricPlaneConnectionId] = message;
            if (state.CompletedTargets.Count == state.Targets.Count)
            {
                await CompletePlaneRequestAsync(state);
            }
        }, ct);
    }
    private async Task CompletePlaneRequestAsync(PlaneSendRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: CompleteRequestAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenPlaneSendRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptions = state.CompletedTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_PlaneSendRequestDone_ToApiAsync(
            new PlaneSendRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage == null
                ? null
                : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }

    public async Task Receive_PlaneInvokeRequest_FromApiAsync(FabricPlaneHost caller, PlaneInvokeRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_PlaneInvokeRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            (var fabricHosts, var actor) = ResolvePlaneTargets(request.Routing, receiveSize);

            if (fabricHosts.Length == 0)
            {
                if (request.Routing.ThrowIfNoReceiver)
                    await caller.Send_PlaneInvokeRequestAck_ToApiAsync(
                        new PlaneInvokeRequestAckDto(
                            request.Routing,
                            false,
                            false,
                            new ExceptionDto($"No fabric connections found with ToServiceSubscriptionId {request.Routing.ToServiceSubscriptionId}, service: {request.Routing.ServiceId} method: {request.Routing.MethodId}", null, Environment.StackTrace)),
                        actor);
                else
                    await caller.Send_PlaneInvokeRequestAck_ToApiAsync(
                        new PlaneInvokeRequestAckDto(
                            request.Routing,
                            false,
                            false,
                            null),
                        actor);
                return;
            }

            var state = new PlaneInvokeRequestState
            {
                Routing = request.Routing,
                Actor = actor,
                Caller = caller,
                Targets = new ConcurrentDictionary<FabricPlaneConnectionId, FabricPlaneHost>(
                    fabricHosts.ToDictionary(a => a.FabricPlaneConnectionId))
            };

            if (!OpenPlaneInvokeRequests.TryAdd(request.Routing.RequestId, state))
                return;

            state.StartTimeout(request.Routing.Timeout, async () =>
            {
                state.ReadyTargets[state.Caller.FabricPlaneConnectionId] = new PlaneInvokeRequestAckDto(
                    request.Routing,
                    false,
                    false,
                    new ExceptionDto("Invoke request timed out.", null, Environment.StackTrace));
                await ReadyPlaneInvokeAsync(state);
            });

            if (fabricHosts.Length == 0)
            {
                await ReadyPlaneInvokeAsync(state);
                return;
            }

            foreach (var host in fabricHosts)
                await host.Send_PlaneInvokeRequest_ToApiAsync(request, actor);
        }, ct);
    }
    public async Task Receive_PlaneInvokeRequestCancelled_FromApiAsync(FabricPlaneHost caller, PlaneInvokeRequestCancelledDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_PlaneInvokeRequestCancelled_FromApiAsync({cancel}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenPlaneInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor.EnqueueReceive(receiveSize);

            foreach (var host in state.Targets.Values)
                await host.Send_PlaneInvokeRequestCancelled_ToApiAsync(message, state.Actor);

            state.Cancel();
            if (state.Targets.Count == 0)
            {
                OpenPlaneInvokeRequests.TryRemove(message.Routing.RequestId, out _);
                state.TryComplete();
            }
        }, ct);
    }
    public async Task Receive_PlaneInvokeRequestAck_FromApiAsync(FabricPlaneHost caller, PlaneInvokeRequestAckDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_PlaneInvokeRequestAckAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenPlaneInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.ReadyTargets[caller.FabricPlaneConnectionId] = message;
            if (state.ReadyTargets.Count == state.Targets.Count)
            {
                await ReadyPlaneInvokeAsync(state);
            }
        }, ct);
    }
    public async Task Receive_PlaneInvokeRequestDone_FromApiAsync(FabricPlaneHost caller, PlaneInvokeRequestDoneDto message, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_PlaneInvokeRequestDoneAsync({done}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), message, receiveSize);

            if (!OpenPlaneInvokeRequests.TryGetValue(message.Routing.RequestId, out var state))
                return;

            state.ResetTimeout();
            state.Actor?.EnqueueReceive(receiveSize);

            state.CompletedTargets[caller.FabricPlaneConnectionId] = message;
            if (state.CompletedTargets.Count == state.Targets.Count)
            {
                await CompletePlaneInvokeAsync(state);
            }
        }, ct);
    }
    private async Task ReadyPlaneInvokeAsync(PlaneInvokeRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryReady())
            return;


        if (state.ReadyTargets.Values.All(a => a.EnumeratorCreated == false))
        {
            if (state.Routing.ThrowIfNoReceiver)
                await state.Caller.Send_PlaneInvokeRequestAck_ToApiAsync(
                    new PlaneInvokeRequestAckDto(
                        state.Routing,
                        false,
                        false,
                        new ExceptionDto($"No fabric connections found with userId {state.Routing.ToPlaneConnectionId} or sessionId {state.Routing.ToServiceSubscriptionId}, service: {state.Routing.ServiceId} method: {state.Routing.MethodId}", null, Environment.StackTrace)),
                    state.Actor);
            else
                await state.Caller.Send_PlaneInvokeRequestAck_ToApiAsync(
                    new PlaneInvokeRequestAckDto(
                        state.Routing,
                        false,
                        false,
                        null),
                    state.Actor);
            return;
        }


        var exceptions = state.ReadyTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_PlaneInvokeRequestAck_ToApiAsync(
            new PlaneInvokeRequestAckDto(
                state.Routing,
                true,
                false,
                exceptionMessage == null
                ? null
                : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }
    private async Task CompletePlaneInvokeAsync(PlaneInvokeRequestState state)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: ReadyInvokeAsync({state})", DateTime.Now.ToString("HH:mm:ss.fff"), state);

        if (!state.TryComplete())
            return;

        OpenPlaneInvokeRequests.TryRemove(state.Routing.RequestId, out _);

        var exceptions = state.CompletedTargets.Values.Where(a => a.Exception != null).Select(a => a.Exception!).ToArray();
        var exceptionMessage =
            exceptions.Length == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Select(a => a.Message));

        var exceptionInnerMessage =
            exceptions.Count(a => a.InnerMessage != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.InnerMessage != null).Select(a => a.InnerMessage));

        var exceptionStackTrace =
            exceptions.Count(a => a.StackTrace != null) == 0
                ? null
                : string.Join("\r\n\r\n===\r\n\r\n", exceptions.Where(a => a.StackTrace != null).Select(a => a.StackTrace));

        await state.Caller.Send_PlaneInvokeRequestDone_ToApiAsync(
            new PlaneInvokeRequestDoneDto(
                state.Routing,
                state.Cancelled,
                exceptionMessage == null
                ? null
                : new ExceptionDto(exceptionMessage, exceptionInnerMessage, exceptionStackTrace)
            ), state.Actor);
    }

    public async Task Receive_PlaneStreamingRequest_FromApiAsync(FabricPlaneHost caller, PlaneStreamingRequestDto request, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_PlaneStreamingRequest_FromApiAsync({request}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), request, receiveSize);

            if (OpenPlaneSendRequests.TryGetValue(request.Routing.RequestId, out var state))
            {
                state.ResetTimeout();
                state.Actor.EnqueueReceive(receiveSize);

                if (state.StreamRoutes.TryAdd(request.StreamId, caller))
                {
                    if (request.ArgumentIndex < 0)
                    {
                        foreach (var target in state.Targets.Values)
                        {
                            await target.Send_PlaneStreamingRequest_ToApiAsync(request, state.Actor);
                        }
                    }
                    else
                    {
                        await state.Caller.Send_PlaneStreamingRequest_ToApiAsync(request, state.Actor);
                    }
                }
            }
            if (OpenPlaneInvokeRequests.TryGetValue(request.Routing.RequestId, out var state2))
            {
                state2.ResetTimeout();
                state2.Actor.EnqueueReceive(receiveSize);

                if (state2.StreamRoutes.TryAdd(request.StreamId, caller))
                {
                    if (request.ArgumentIndex < 0)
                    {
                        foreach (var target in state2.Targets.Values)
                        {
                            await target.Send_PlaneStreamingRequest_ToApiAsync(request, state2.Actor);
                        }
                    }
                    else
                    {
                        await state2.Caller.Send_PlaneStreamingRequest_ToApiAsync(request, state2.Actor);
                    }
                }
            }
        }, ct);
    }
    public async Task Receive_PlaneStreamingResponse_FromApiAsync(FabricPlaneHost caller, PlaneStreamingResponseDto response, long receiveSize, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Receive_PlaneStreamingResponse_FromApiAsync({response}, {receiveSize})", DateTime.Now.ToString("HH:mm:ss.fff"), response, receiveSize);

            if (OpenPlaneSendRequests.TryGetValue(response.Routing.RequestId, out var state))
            {
                state.ResetTimeout();
                state.Actor.EnqueueReceive(receiveSize);

                if (state.StreamRoutes.TryRemove(response.StreamId, out var sender))
                {
                    await sender.Send_PlaneStreamingResponse_ToApiAsync(response, state.Actor);
                }
            }

            if (OpenPlaneInvokeRequests.TryGetValue(response.Routing.RequestId, out var state2))
            {
                state2.ResetTimeout();
                state2.Actor.EnqueueReceive(receiveSize);

                if (state2.StreamRoutes.TryRemove(response.StreamId, out var sender))
                {
                    await sender.Send_PlaneStreamingResponse_ToApiAsync(response, state2.Actor);
                }
            }

        }, ct);
    }

    private (FabricPlaneHost[], IActor) ResolvePlaneTargets(PlaneRoutingDto routing, long receiveSize)
    {
        var service = Services[routing.ServiceId];
        service.EnqueueReceive(receiveSize);

        if (routing.To == PlaneToChoice.ToPlaneConnection && routing.ToPlaneConnectionId != null)
        {
            if (service.PlaneConnections.TryGetValue(routing.ToPlaneConnectionId, out var target) == false)
                return ([], service);
            return ([target.Connection], target);
        }
        if (routing.To == PlaneToChoice.ToServiceSubscription && routing.ToServiceSubscriptionId != null)
        {
            if (service.PlaneServiceSubscriptions.TryGetValue(routing.ToServiceSubscriptionId, out var target) == false)
                return ([], service);
            return ([target.Connection], target);
        }
        var subscribers = service.PlaneConnections.Values.Select(a => a.Connection);
        var hosts = subscribers.Distinct().ToArray();
        if (hosts.Length == 0)
            return ([], service);

        if (routing.To == PlaneToChoice.ToAll)
        {
            return (hosts, service);
        }

        // ToAny => Round-robin
        var idx = (uint)Interlocked.Increment(ref _roundRobinIndex) % (uint)hosts.Length;
        return ([hosts[idx]], service);
    }

    #endregion

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
        var connections2 = AllPlaneConnections
            .Select(connection =>
            {
                var send = connection.GetSendBytesPerSecond();
                var receive = connection.GetReceiveBytesPerSecond();
                return new FabricConnectionSnapshot(
                    "Plane #" + connection.FabricPlaneConnectionId.Value.ToString(),
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
                foreach (var session in service.Sessions.Values)
                {
                    var sendSession = session.GetSendSpeed();
                    sendSpeed += sendSession.bytes;
                    sendCount += sendSession.count;
                }
                foreach (var user in service.Users.Values)
                {
                    var sendUser = user.GetSendSpeed();
                    sendSpeed += sendUser.bytes;
                    sendCount += sendUser.count;
                }
                foreach (var busconnection in service.PlaneConnections.Values)
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
                foreach (var session in service.Sessions.Values)
                {
                    var receivedSession = session.GetReceiveSpeed();
                    receivedSpeed += receivedSession.bytes;
                    receivedCount += receivedSession.count;
                }
                foreach (var user in service.Users.Values)
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

    public async Task Receive_Log_FromApiAsync(FabricHost fabricHost, LogDto log, long receiveSize, CancellationToken ct)
    {
        try
        {
            if (log.Category == null)
                return;

            var logger = LoggerFactory.CreateLogger(log.Category);

            var originalFormat = $"{(log.Source ?? "").ToUpper()}{(log.Source == null ? "" : " ")}{(log.MessageTemplate ?? log.Message)}";
            // Reconstruct structured logging state
            var state = new List<KeyValuePair<string, object?>>
            {
                new("{OriginalFormat}", originalFormat)
            };

            foreach (var property in log.Properties)
                state.Add(new(property.Key, property.Value));

            // Reconstruct exception (if available)
            Exception? exception = log.ExceptionDetails != null
                ? new RemoteException(
                    log.ExceptionDetails,
                    null, // inner exception?
                    null) // stacktrace?
                : null;

            // Log locally on the server
            logger.Log(
                log.Level,
                new EventId(log.EventId, log.EventName),
                state,
                exception,
                (_, _) => log.Message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Receive_Log_FromClientAsync failed");
        }
    }

}
