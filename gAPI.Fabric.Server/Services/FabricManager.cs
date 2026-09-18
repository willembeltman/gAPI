using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.Fabric.Server.Services;

public class FabricManager
{
    private readonly SessionCache SessionCache;

    public readonly FabricManagerId FabricManagerId;
    public readonly FabricHostCollection Connections;
    public readonly ServiceCollection Services;
    public readonly ConcurrentDictionary<RequestId, RequestState> OpenRequests;
    public readonly IConsole Console;
    public event EventHandler? OnUpdate;

    public FabricManager(IConsole console)
    {
        FabricManagerId = FabricManagerId.New();
        Console = console;
        SessionCache = new();
        Connections = new();
        Services = new(this);
        OpenRequests = new();
    }

    public void StartNewFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricHost = new FabricHost(this, tcpClient);
        fabricHost.Start();

        OnUpdate?.Invoke(this, new EventArgs());
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
            await caller.Send_GetSessionCookieDataResponse_ToApiAsync(getSessionCookieDataResponse, null);
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

            OnUpdate?.Invoke(this, new EventArgs());
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

            OnUpdate?.Invoke(this, new EventArgs());
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
                return;

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
                return;

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
            state.Actor?.EnqueueReceive(receiveSize);

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
            state.Actor?.EnqueueReceive(receiveSize);

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
            state.Actor?.EnqueueReceive(receiveSize);

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
            state.Actor?.EnqueueReceive(receiveSize);

            if (state.StreamRoutes.TryRemove(response.StreamId, out var sender))
            {
                await sender.Send_StreamingResponseServerToClient_ToApiAsync(response, state.Actor);
            }
        }, ct);
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var conn in Connections)
            conn.Dispose();

        OnUpdate?.Invoke(this, new EventArgs());
    }

    public async Task DisposeAsync()
    {
        await DisconnectAllAsync();
    }
}