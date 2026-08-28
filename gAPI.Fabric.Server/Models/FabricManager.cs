using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Collections;
using gAPI.Fabric.Server.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Models;

public class FabricManager
{
    public readonly WssSessionCache SessionCache;
    public readonly FabricHostCollection Connections;
    public readonly ServiceCollection Services;
    public readonly ConcurrentDictionary<RequestId, InvokeRequestState> InvokeRequests = new();
    public readonly IConsole Console;

    public FabricManager(IConsole console)
    {
        Console = console;
        SessionCache = new WssSessionCache();
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

    public async Task UpdateSessionAsync(FabricHost fabricHost, UpdateSessionDto updateSession, long receiveSize, CancellationToken token)
    {
        SessionCache.AddOrUpdate(updateSession.SessionId, updateSession.CookieData);
    }

    public async Task ClearSessionAsync(FabricHost fabricHost, SendClearSessionDto clearSession, long receiveSize, CancellationToken token)
    {
        SessionCache.Remove(clearSession.SessionId);
    }

    public async Task GetSessionCookieDataAsync(FabricHost fabricHost, SendGetSessionCookieDataDto getSessionCookieData, long receiveSize, CancellationToken token)
    {
        var sessionId = getSessionCookieData.SessionId;
        string? cookieData = null;
        SessionCache.TryGet(sessionId, out cookieData);
        var getSessionCookieDataResponse = new SendGetSessionCookieDataResponseDto(sessionId, cookieData);
        await fabricHost.SendGetSessionCookieDataResponseAsync(getSessionCookieDataResponse, null); // todo dat actor spul
    }

    public async Task SubscribeAsync(FabricHost caller, SubscribeDto subscribe, long receiveSize, CancellationToken ct)
    {
        await Services[subscribe.ServiceId]
            .Subscribe(caller, subscribe.UserId, subscribe.SessionId, receiveSize);

        OnUpdate?.Invoke(this, new EventArgs());
    }
    public async Task UnsubscribeAsync(FabricHost caller, UnsubscribeDto unsubscribe, long receiveSize, CancellationToken ct)
    {
        await Services[unsubscribe.ServiceId]
            .Unsubscribe(caller, unsubscribe.UserId, unsubscribe.SessionId, receiveSize);

        OnUpdate?.Invoke(this, new EventArgs());
    }

    public async Task SendRequestAsync(FabricHost caller, SendRequestDto request, long receiveSize, CancellationToken ct)
    {
        (var fabricHosts, var actor) = Services[request.ServiceId].GetFabricHosts(request.UserId, request.SessionId);
        actor.EnqueueReceive(receiveSize);
        foreach (var fabricHost in fabricHosts)
        {
            await fabricHost.SendRequestAsync(request, actor);
        }
    }

    public async Task InvokeRequestAsync(FabricHost caller, InvokeRequestDto invokeRequest, long receiveSize, CancellationToken ct)
    {
        (var fabricHosts2, var actor) = Services[invokeRequest.ServiceId]
            .GetFabricHosts(invokeRequest.UserId, invokeRequest.SessionId);
        var fabricHosts = fabricHosts2.ToArray();
        actor.EnqueueReceive(receiveSize);
        if (fabricHosts.Length == 0)
            return;

        var state = new InvokeRequestState
        {
            Actor = actor,
            RequestId = invokeRequest.RequestId,
            Caller = caller,
            PendingHosts = [.. fabricHosts.Select(h => h.Id)]
        };

        if (!InvokeRequests.TryAdd(invokeRequest.RequestId, state))
            return;

        _ = StartTimeoutAsync(state, TimeSpan.FromSeconds(10));

        foreach (var host in fabricHosts)
            await host.InvokeRequestAsync(invokeRequest, actor);
    }
    public async Task InvokeResponseAsync(FabricHost caller, InvokeResponseDto invokeResponse, long receiveSize, CancellationToken ct)
    {
        if (!InvokeRequests.TryGetValue(invokeResponse.RequestId, out var state))
            return; // timeout / already completed
        state.Actor?.EnqueueReceive(receiveSize);
        // DIRECT doorsluizen
        await state.Caller.InvokeResponseAsync(invokeResponse, state.Actor);
    }
    public async Task InvokeResponseDoneAsync(FabricHost fabricHost, RequestId requestId, long receiveSize, CancellationToken ct)
    {
        if (!InvokeRequests.TryGetValue(requestId, out var state))
            return;

        state.Actor?.EnqueueReceive(receiveSize);

        bool isLast;

        lock (state)
        {
            state.PendingHosts.Remove(fabricHost.Id);
            isLast = state.PendingHosts.Count == 0;
        }

        if (isLast)
            await CompleteRequestAsync(state);
    }
    private async Task StartTimeoutAsync(InvokeRequestState state, TimeSpan timeout)
    {
        try
        {
            await Task.Delay(timeout, state.TimeoutCts.Token);
            await CompleteRequestAsync(state);
        }
        catch (TaskCanceledException)
        {
            // normaal pad
        }
    }
    private async Task CompleteRequestAsync(InvokeRequestState state)
    {
        if (!state.TryComplete())
            return;

        state.TimeoutCts.Cancel();
        InvokeRequests.TryRemove(state.RequestId, out _);

        await state.Caller.InvokeResponseDoneAsync(state.RequestId, state.Actor);
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var conn in Connections)
            conn.Dispose();

        RaiseUpdate(this);
    }

    public async Task DisposeAsync()
    {
        await DisconnectAllAsync();
    }

    public void RaiseUpdate(object obj)
    {
        OnUpdate?.Invoke(obj, new EventArgs());
    }

}