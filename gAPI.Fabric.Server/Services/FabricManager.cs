using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Models;
using System.Collections.Concurrent;
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
        await caller.Send_GetSessionCookieDataResponse_ToApiAsync(getSessionCookieDataResponse, null); // todo dat actor spul
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

    //public async Task Send_SendRequest_ToServiceAsync(FabricHost caller, SendRequestDto request, long receiveSize, CancellationToken ct)
    //{
    //    (var fabricHosts, var actor) = Services[request.ServiceId].GetFabricHosts(request.UserId, request.SessionId);
    //    actor.EnqueueReceive(receiveSize);
    //    foreach (var fabricHost in fabricHosts)
    //    {
    //        await fabricHost.Send_SendRequest_ToServiceAsync(request, actor);
    //    }
    //}

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
            Targets = [.. fabricHosts.Select(host => host.FabricConnectionId)]
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
    public async Task Receive_SendRequestCancelled_FromApiAsync(FabricHost fabricHost, SendRequestCancelledDto sendRequestCancelled, long receiveSize, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    public async Task Receive_SendRequestDone_FromApiAsync(FabricHost caller, SendRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
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
            if (done.StateIsChanged)
            {
                state.StateIsChanged = true;
                state.StateData = done.StateData;
            }
        }

        if (state.CompletedTargets.Count == state.Targets.Count)
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
                    state.StateIsChanged,
                    state.StateData,
                    null
                ), state.Actor);
        }
        else
        {
            var exceptionMessage = string.Join(", ", state.Exceptions.Values);
            await state.Caller.Send_SendRequestDone_ToApiAsync(
                new SendRequestDoneDto(
                    state.RequestId,
                    state.StateIsChanged,
                    state.StateData,
                    exceptionMessage
                ), state.Actor);
        }
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
        foreach (var targetId in state.Targets)
        {
            var target = Connections.FirstOrDefault(host => host.FabricConnectionId == targetId);
            if (target != null)
                await target.Send_StreamingResponse_ToApiAsync(response, state.Actor);
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
            Targets = [.. fabricHosts.Select(host => host.FabricConnectionId)]
            //PendingHosts = [.. fabricHosts.Select(h => h.Id)]
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
    public async Task Receive_InvokeRequestCancelled_FromApiAsync(FabricHost fabricHost, InvokeRequestCancelledDto invokeRequestCancelled, long receiveSize, CancellationToken token)
    {
        throw new NotImplementedException();
    }
    //public async Task Receive_InvokeResponseAsync(FabricHost caller, InvokeResponseDto response, long receiveSize, CancellationToken ct)
    //{
    //    if (!InvokeRequests.TryGetValue(response.RequestId, out var state))
    //        return; // timeout / already completed
    //    state.ResetTimeout();
    //    state.Actor?.EnqueueReceive(receiveSize);
    //    if (response.StateIsChanged)
    //    {
    //        state.StateIsChanged = true;
    //        state.StateData = response.StateData;
    //    }
    //    // DIRECT doorsluizen
    //    await state.Caller.Send_InvokeResponse_ToApiAsync(response, state.Actor);
    //}
    public async Task Receive_InvokeRequestDoneAsync(FabricHost caller, InvokeRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        if (!InvokeRequests.TryGetValue(done.Routing, out var state))
            return;

        state.ResetTimeout();
        state.Actor?.EnqueueReceive(receiveSize);
        lock (state)
        {
            state.StreamIds.AddRange(done.StreamIds);
            state.CompletedTargets.Add(caller.FabricConnectionId);
            //if (done.ExceptionMessage != null)
            //{
            //    state.Exceptions.Add(caller.FabricConnectionId, done.ExceptionMessage);
            //}
        }

        if (state.CompletedTargets.Count == state.Targets.Count)
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
                [.. state.StreamIds]
            ), state.Actor);
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