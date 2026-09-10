using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Models;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Services;

public class FabricManager
{
    public readonly FabricManagerId FabricManagerId;
    public readonly SessionCache SessionCache;
    public readonly FabricHostCollection Connections;
    public readonly ServiceCollection Services;
    public readonly ConcurrentDictionary<RoutingDto, RequestState> SendRequests;
    public readonly ConcurrentDictionary<RoutingDto, RequestState> InvokeRequests;
    public readonly IConsole Console;

    public FabricManager(IConsole console)
    {
        FabricManagerId = FabricManagerId.New();
        Console = console;
        SessionCache = new();
        Connections = new();
        Services = new(this);
        SendRequests = new();
        InvokeRequests = new();
    }

    public event EventHandler? OnUpdate;

    public void StartNewFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricHost = new FabricHost(this, tcpClient);
        fabricHost.Start();

        OnUpdate?.Invoke(this, new EventArgs());
    }

    public async Task Receive_UpdateSession_FromApiAsync(FabricHost caller, UpdateSessionDto updateSession, long receiveSize, CancellationToken token)
    {
        SessionCache.AddOrUpdate(updateSession.SessionId, updateSession.CookieData);
    }
    public async Task Receive_ClearSession_FromApiAsync(FabricHost caller, SendClearSessionDto clearSession, long receiveSize, CancellationToken token)
    {
        SessionCache.Remove(clearSession.SessionId);
    }
    public async Task Receive_GetSessionCookieData_FromApiAsync(FabricHost caller, SendGetSessionCookieDataDto getSessionCookieData, long receiveSize, CancellationToken token)
    {
        var sessionId = getSessionCookieData.SessionId;
        string? cookieData = null;
        SessionCache.TryGet(sessionId, out cookieData);
        var getSessionCookieDataResponse = new SendGetSessionCookieDataResponseDto(sessionId, cookieData);
        await caller.Send_GetSessionCookieDataResponse_ToApiAsync(getSessionCookieDataResponse, null);
    }

    public async Task Receive_Subscribe_FromApiAsync(FabricHost caller, SubscribeDto subscribe, long receiveSize, CancellationToken ct)
    {
        await Services[subscribe.ServiceId]
            .Subscribe(caller, subscribe.UserId, subscribe.SessionId, receiveSize);

        OnUpdate?.Invoke(this, new EventArgs());
    }
    public async Task Receive_Unsubscribe_FromApiAsync(FabricHost caller, UnsubscribeDto unsubscribe, long receiveSize, CancellationToken ct)
    {
        await Services[unsubscribe.ServiceId]
            .Unsubscribe(caller, unsubscribe.UserId, unsubscribe.SessionId, receiveSize);

        OnUpdate?.Invoke(this, new EventArgs());
    }

    public async Task Receive_SendRequest_FromApiAsync(FabricHost caller, SendRequestDto request, long receiveSize, CancellationToken ct)
    {
        (var fabricHostsEnumerable, var actor) = Services[request.Routing.ServiceId]
            .GetFabricHosts(request.Routing.UserId, request.Routing.SessionId);
        var fabricHosts = fabricHostsEnumerable.ToArray();
        actor.EnqueueReceive(receiveSize);
        if (fabricHosts.Length == 0)
            return;

        var state = new RequestState
        {
            RequestId = request.Routing,
            Caller = caller,
            Actor = actor,
            Targets = fabricHosts
        };

        if (!SendRequests.TryAdd(request.Routing, state))
            return;

        state.StartTimeout(TimeSpan.FromSeconds(60), () =>
        {
            state.Exceptions.Add(state.Caller.FabricConnectionId, "Request timed out.");
            _ = CompleteRequestAsync(state);
        });

        foreach (var fabricHost in fabricHosts)
            await fabricHost.Send_SendRequest_ToApiAsync(request, actor);
    }
    public async Task Receive_SendRequestCancelled_FromApiAsync(FabricHost caller, SendRequestCancelledDto cancel, long receiveSize, CancellationToken token)
    {
        if (!SendRequests.TryGetValue(cancel.Routing, out var state))
            return;

        foreach(var target in state.Targets)
        {
            await target.Send_SendRequestCancelled_ToApiAsync(cancel, state.Actor);
        }
    }
    public async Task Receive_SendRequestDone_FromApiAsync(FabricHost caller, SendRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        // Optie 1:
        // Client


        if (!SendRequests.TryGetValue(done.Routing, out var state))
            return;

        state.ResetTimeout();
        state.Actor.EnqueueReceive(receiveSize);
        lock (state)
        {
            state.CompletedTargets.Add(caller.FabricConnectionId);
            if (done.ExceptionMessage != null)
            {
                state.Exceptions.Add(caller.FabricConnectionId, done.ExceptionMessage);
            }
            if (done.Cancelled)
            {
                state.Cancelled = true;
            }
        }

        if (state.CompletedTargets.Count == state.Targets.Length)
        {
            await CompleteRequestAsync(state);
        }
    }
    private async Task CompleteRequestAsync(RequestState state)
    {
        if (!state.TryComplete())
            return;

        SendRequests.TryRemove(state.RequestId, out _);

        if (state.Exceptions.Count == 0)
        {
            await state.Caller.Send_SendRequestDone_ToApiAsync(
                new SendRequestDoneDto(
                    state.RequestId,
                    false,
                    null
                ), state.Actor);
        }
        else
        {
            var exceptionMessage = string.Join(", ", state.Exceptions.Values);
            await state.Caller.Send_SendRequestDone_ToApiAsync(
                new SendRequestDoneDto(
                    state.RequestId,
                    false,
                    exceptionMessage
                ), state.Actor);
        }
    }

    public async Task Receive_InvokeRequest_FromApiAsync(FabricHost caller, InvokeRequestDto request, long receiveSize, CancellationToken ct)
    {
        (var fabricHostsEnumerable, var actor) = Services[request.Routing.ServiceId]
            .GetFabricHosts(request.Routing.UserId, request.Routing.SessionId);
        var fabricHosts = fabricHostsEnumerable.ToArray();
        actor.EnqueueReceive(receiveSize);
        if (fabricHosts.Length == 0)
            return;

        var state = new RequestState
        {
            RequestId = request.Routing,
            Actor = actor,
            Caller = caller,
            Targets = fabricHosts
        };

        if (!InvokeRequests.TryAdd(request.Routing, state))
            return;

        state.StartTimeout(TimeSpan.FromSeconds(60), () =>
        {
            state.Exceptions.Add(state.Caller.FabricConnectionId, "Invoke request timed out.");
            _ = CompleteInvokeAsync(state);
        });

        foreach (var host in fabricHosts)
            await host.Send_InvokeRequest_ToApiAsync(request, actor);
    }
    public async Task Receive_InvokeRequestCancelled_FromApiAsync(FabricHost fabricHost, InvokeRequestCancelledDto cancel, long receiveSize, CancellationToken token)
    {
        if (!SendRequests.TryGetValue(cancel.Routing, out var state))
            return;

        foreach (var target in state.Targets)
        {
            await target.Send_InvokeRequestCancelled_ToApiAsync(cancel, state.Actor);
        }
    }
    public async Task Receive_InvokeRequestDoneAsync(FabricHost caller, InvokeRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        if (!InvokeRequests.TryGetValue(done.Routing, out var state))
            return;

        state.ResetTimeout();
        state.Actor?.EnqueueReceive(receiveSize);
        lock (state)
        {
            //state.StreamIds.AddRange(done.StreamIds);
            state.CompletedTargets.Add(caller.FabricConnectionId);
            if (done.ExceptionMessage != null)
            {
                state.Exceptions.Add(caller.FabricConnectionId, done.ExceptionMessage);
            }
            if (done.Cancelled)
            {
                state.Cancelled = true;
            }
        }

        if (state.CompletedTargets.Count == state.Targets.Length)
        {
            await CompleteInvokeAsync(state);
        }
    }
    private async Task CompleteInvokeAsync(RequestState state)
    {
        if (!state.TryComplete())
            return;

        InvokeRequests.TryRemove(state.RequestId, out _);

        await state.Caller.Send_InvokeRequestDone_ToApiAsync(
            new InvokeRequestDoneDto(
                state.RequestId,
                state.Cancelled, 
                state.Exceptions.Count == 0 ? null : string.Join(", ", state.Exceptions.Values)
            ), state.Actor);
    }

    public async Task Receive_StreamingRequest_FromApiAsync(FabricHost caller, StreamingRequestDto request, long receiveSize, CancellationToken ct)
    {
        if (!SendRequests.TryGetValue(request.Routing, out var state))
            if (!InvokeRequests.TryGetValue(request.Routing, out state))
                return;

        state.ResetTimeout();
        state.Actor?.EnqueueReceive(receiveSize);
        await state.Caller.Send_StreamingRequest_ToApiAsync(request, state.Actor);
    }
    public async Task Receive_StreamingResponse_FromApiAsync(FabricHost caller, StreamingResponseDto response, long receiveSize, CancellationToken ct)
    {
        if (!SendRequests.TryGetValue(response.Routing, out var state))
            if (!InvokeRequests.TryGetValue(response.Routing, out state))
                return;

        state.ResetTimeout();
        state.Actor?.EnqueueReceive(receiveSize);
        foreach (var target in state.Targets)
        {
            await target.Send_StreamingResponse_ToApiAsync(response, state.Actor);
        }
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