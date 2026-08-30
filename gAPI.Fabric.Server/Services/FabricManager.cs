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
    public readonly SessionCache SessionCache;
    public readonly FabricHostCollection Connections;
    public readonly ServiceCollection Services;
    public readonly ConcurrentDictionary<RequestId, RequestState> SendRequests = new();
    public readonly ConcurrentDictionary<RequestId, RequestState> InvokeRequests = new();
    public readonly IConsole Console;

    public FabricManager(IConsole console)
    {
        Console = console;
        SessionCache = new();
        Connections = new(this);
        Services = new(this);
    }

    public event EventHandler? OnUpdate;

    public void StartNewFabricHost(TcpClient tcpClient)
    {
        // FabricHost abonneert zichzelf op connections
        var fabricHost = new FabricHost(
            this,
            tcpClient);
        fabricHost.Start();

        OnUpdate?.Invoke(this, new EventArgs());
    }

    public async Task Receive_UpdateSessionAsync(FabricHost caller, UpdateSessionDto updateSession, long receiveSize, CancellationToken token)
    {
        SessionCache.AddOrUpdate(updateSession.SessionId, updateSession.CookieData);
    }
    public async Task Receive_ClearSessionAsync(FabricHost caller, SendClearSessionDto clearSession, long receiveSize, CancellationToken token)
    {
        SessionCache.Remove(clearSession.SessionId);
    }
    public async Task Receive_GetSessionCookieDataAsync(FabricHost caller, SendGetSessionCookieDataDto getSessionCookieData, long receiveSize, CancellationToken token)
    {
        var sessionId = getSessionCookieData.SessionId;
        string? cookieData = null;
        SessionCache.TryGet(sessionId, out cookieData);
        var getSessionCookieDataResponse = new SendGetSessionCookieDataResponseDto(sessionId, cookieData);
        await caller.Send_GetSessionCookieDataResponse_ToApiAsync(getSessionCookieDataResponse, null); // todo dat actor spul
    }

    public async Task Receive_SubscribeAsync(FabricHost caller, SubscribeDto subscribe, long receiveSize, CancellationToken ct)
    {
        await Services[subscribe.ServiceId]
            .Subscribe(caller, subscribe.UserId, subscribe.SessionId, receiveSize);

        OnUpdate?.Invoke(this, new EventArgs());
    }
    public async Task Receive_UnsubscribeAsync(FabricHost caller, UnsubscribeDto unsubscribe, long receiveSize, CancellationToken ct)
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

    public async Task Receive_Send_SendRequest_ToServiceAsync(FabricHost caller, SendRequestDto request, long receiveSize, CancellationToken ct)
    {
        (var fabricHostsEnumerable, var actor) = Services[request.ServiceId]
            .GetFabricHosts(request.UserId, request.SessionId);
        var fabricHosts = fabricHostsEnumerable.ToArray();
        actor.EnqueueReceive(receiveSize);
        if (fabricHosts.Length == 0)
            return;

        var state = new RequestState
        {
            RequestId = request.RequestId,
            Caller = caller,
            Actor = actor,
            Targets = [.. fabricHosts.Select(host => host.Id)]
        };

        if (!SendRequests.TryAdd(request.RequestId, state))
            return;

        _ = StartRequestTimeoutAsync(state, TimeSpan.FromSeconds(60));
        foreach (var fabricHost in fabricHosts)
            await fabricHost.Send_SendRequest_ToApiAsync(request, actor);
    }
    public async Task Receive_SendRequestDoneAsync(FabricHost caller, SendRequestDoneDto done, long receiveSize, CancellationToken ct)
    {
        if (!SendRequests.TryGetValue(done.RequestId, out var state))
            return;

        state.Actor.EnqueueReceive(receiveSize);
        lock (state)
        {
            state.CompletedTargets.Add(caller.Id);
        }

        if (state.CompletedTargets.Count == state.Targets.Count)
        {
            await CompleteRequestAsync(state);
        }
    }
    public async Task Receive_SendRequestExceptionAsync(FabricHost caller, SendRequestExceptionDto ex, long receiveSize, CancellationToken ct)
    {
        if (!SendRequests.TryGetValue(ex.RequestId, out var state))
            return;

        state.Actor.EnqueueReceive(receiveSize);

        lock (state)
        {
            state.CompletedTargets.Add(caller.Id);
            state.Exceptions.Add(caller.Id, ex.ExceptionMessage);
        }

        if (state.CompletedTargets.Count != state.Targets.Count)
            return;

        await CompleteRequestAsync(state);

    }
    private async Task StartRequestTimeoutAsync(RequestState state, TimeSpan timeout)
    {
        try
        {
            await Task.Delay(timeout, state.TimeoutCts.Token);
            state.Exceptions.Add(state.Caller.Id, "Request timed out.");
            await CompleteRequestAsync(state);
        }
        catch (TaskCanceledException)
        {
        }
    }
    private async Task CompleteRequestAsync(RequestState state)
    {
        if (!state.TryComplete())
            return;

        state.TimeoutCts.Cancel();
        state.TimeoutCts.Dispose();
        InvokeRequests.TryRemove(state.RequestId, out _);

        if (state.Exceptions.Count == 0)
        {
            await state.Caller.Send_SendRequestDone_ToApiAsync(new SendRequestDoneDto()
            {
                RequestId = state.RequestId
            }, state.Actor);
        }
        else
        {
            var exceptionMessage = string.Join(", ", state.Exceptions.Values);
            await state.Caller.Send_SendRequestDone_ToApiAsync(new SendRequestExceptionDto()
            {
                RequestId = state.RequestId,
                ExceptionMessage = exceptionMessage
            }, state.Actor);
        }
    }

    public async Task Receive_InvokeArgumentRequestAsync(FabricHost caller, InvokeArgumentRequestDto request, long receiveSize, CancellationToken ct)
    {
        if (!SendRequests.TryGetValue(request.RequestId, out var state))
            if (!InvokeRequests.TryGetValue(request.RequestId, out state))
                return;

        state.Actor?.EnqueueReceive(receiveSize);
        await state.Caller.Send_InvokeArgumentRequest_ToApiAsync(request, state.Actor);
    }
    public async Task Receive_InvokeArgumentResponseAsync(FabricHost caller, InvokeArgumentResponseDto response, long receiveSize, CancellationToken ct)
    {
        if (!SendRequests.TryGetValue(response.RequestId, out var state))
            if (!InvokeRequests.TryGetValue(response.RequestId, out state))
                return;

        state.Actor?.EnqueueReceive(receiveSize);
        foreach (var targetId in state.Targets)
        {
            var target = Connections.FirstOrDefault(host => host.Id == targetId);
            if (target != null)
                await target.Send_InvokeArgumentResponse_ToApiAsync(response, state.Actor);
        }

    }

    public async Task Receive_InvokeRequest_FromApiAsync(FabricHost caller, InvokeRequestDto request, long receiveSize, CancellationToken ct)
    {
        (var fabricHostsEnumerable, var actor) = Services[request.ServiceId]
            .GetFabricHosts(request.UserId, request.SessionId);
        var fabricHosts = fabricHostsEnumerable.ToArray();
        actor.EnqueueReceive(receiveSize);
        if (fabricHosts.Length == 0)
            return;

        var state = new RequestState
        {
            Actor = actor,
            RequestId = request.RequestId,
            Caller = caller,
            Targets = [.. fabricHosts.Select(host => host.Id)]
            //PendingHosts = [.. fabricHosts.Select(h => h.Id)]
        };

        if (!InvokeRequests.TryAdd(request.RequestId, state))
            return;

        _ = StartInvokeTimeoutAsync(state, TimeSpan.FromSeconds(10));

        foreach (var host in fabricHosts)
            await host.Send_InvokeRequest_ToApiAsync(request, actor);
    }
    public async Task Receive_InvokeResponseAsync(FabricHost caller, InvokeResponseDto response, long receiveSize, CancellationToken ct)
    {
        if (!InvokeRequests.TryGetValue(response.RequestId, out var state))
            return; // timeout / already completed
        state.Actor?.EnqueueReceive(receiveSize);
        // DIRECT doorsluizen
        await state.Caller.Send_InvokeResponse_ToApiAsync(response, state.Actor);
    }
    public async Task Receive_InvokeResponseDoneAsync(FabricHost caller, InvokeResponseDoneDto done, long receiveSize, CancellationToken ct)
    {
        if (!InvokeRequests.TryGetValue(done.RequestId, out var state))
            return;

        state.Actor?.EnqueueReceive(receiveSize);
        lock (state)
        {
            state.CompletedTargets.Add(caller.Id);
        }

        if (state.CompletedTargets.Count == state.Targets.Count)
        {
            await CompleteInvokeAsync(state);
        }
    }
    public async Task Receive_InvokeResponseExceptionAsync(FabricHost caller, InvokeResponseExceptionDto ex, long receiveSize, CancellationToken ct)
    {
        if (!InvokeRequests.TryGetValue(ex.RequestId, out var state))
            return;

        state.Actor?.EnqueueReceive(receiveSize);
        lock (state)
        {
            state.CompletedTargets.Add(caller.Id);
        }

        if (state.CompletedTargets.Count == state.Targets.Count)
        {
            await CompleteInvokeAsync(state);
        }
    }

    private async Task StartInvokeTimeoutAsync(RequestState state, TimeSpan timeout)
    {
        try
        {
            await Task.Delay(timeout, state.TimeoutCts.Token);
            await CompleteInvokeAsync(state);
        }
        catch (TaskCanceledException)
        {
            // normaal pad
        }
    }
    private async Task CompleteInvokeAsync(RequestState state)
    {
        if (!state.TryComplete())
            return;

        state.TimeoutCts.Cancel();
        state.TimeoutCts.Dispose();
        InvokeRequests.TryRemove(state.RequestId, out _);

        if (state.Exceptions.Count == 0)
        {
            await state.Caller.Send_InvokeResponseDone_ToApiAsync(new InvokeResponseDoneDto()
            {
                RequestId = state.RequestId
            }, state.Actor);

        }
        else
        {
            var exceptionMessage = string.Join(", ", state.Exceptions.Values);
            await state.Caller.Send_InvokeResponseException_ToApiAsync(new InvokeResponseExceptionDto()
            {
                RequestId = state.RequestId,
                ExceptionMessage = exceptionMessage
            }, state.Actor);
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