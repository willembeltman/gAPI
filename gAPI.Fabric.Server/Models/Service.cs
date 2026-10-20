using gAPI.Core.Enums;
using gAPI.Core.Ids;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace gAPI.Fabric.Server.Models;

public record Service(ServiceId Id, FabricManager FabricManager) : IActor
{
    public ServiceId Id { get; } = Id;
    public ConcurrentDictionary<FabricConnectionId, FabricHost> Connections { get; } = new();
    public UserCollection Users { get; } = new();
    public SessionCollection Sessions { get; } = new();
    public BusConnectionCollection BusConnections { get; } = new();

    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    private readonly ConcurrentQueue<(double time, long bytes)> SendLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceiveLogger = new();


    public async Task Subscribe(FabricHost connection, UserId userId, SessionId sessionId, long receiveSize)
    {
        Users.TryAdd(userId, connection);
        Sessions.TryAdd(sessionId, connection);
        Connections.TryAdd(connection.FabricConnectionId, connection);
        EnqueueReceive(receiveSize);
    }
    public async Task Unsubscribe(FabricHost connection, UserId userId, SessionId sessionId, long receiveSize)
    {
        Users.TryRemove(userId, connection);
        Sessions.TryRemove(sessionId, connection);
        Connections.TryRemove(connection.FabricConnectionId, out _);
        EnqueueReceive(receiveSize);
    }
    public async Task BusSubscribe(FabricHost connection, BusConnectionId busConnectionId, long receiveSize)
    {
        BusConnections.TryAdd(busConnectionId, connection);
        EnqueueReceive(receiveSize);
    }
    public async Task BusUnsubscribe(FabricHost connection, BusConnectionId busConnectionId, long receiveSize)
    {
        BusConnections.TryRemove(busConnectionId, connection);
        EnqueueReceive(receiveSize);
    }

    public (IEnumerable<FabricHost>, IActor) GetFabricHosts(UserId? userId, SessionId? sessionId)
    {
        if (userId != null)
        {
            var user = Users.TryGet(userId);
            if (user == null) return ([], this);
            return (user.Connections.Values, user);
        }
        else if (sessionId != null)
        {
            var session = Sessions.TryGet(sessionId);
            if (session == null) return ([], this);
            return (session.Connections.Values, session);
        }
        else
        {
            return (Connections.Values, this);
        }
    }
    public (IEnumerable<FabricHost>, IActor) GetFabricHosts(BusToChoice To, BusConnectionId? ToBusConnectionId)
    {
        if (To == BusToChoice.ToAny)
        {
            var busConnection = BusConnections.FirstOrDefault(); // TODO iets met loadbalancing doen
            if (busConnection == null) return ([], this);
            return ([busConnection.Connection], busConnection);
        }
        else if (To == BusToChoice.ToBusConnectionId && ToBusConnectionId != null)
        {
            var busConnection = BusConnections.TryGet(ToBusConnectionId);
            if (busConnection == null) return ([], this);
            return ([busConnection.Connection], busConnection);
        }
        else
        {
            return (BusConnections.Select(a => a.Connection), this);
        }
    }

    private (int count, long bytes) GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        var bytes = 0L;
        var count = 0;
        foreach (var item in queue)
        {
            bytes += item.bytes;
            count++;
        }
        return new(count, bytes);
    }
    public (int count, long bytes) GetSendSpeed() => GetSpeed(SendLogger);
    public (int count, long bytes) GetReceiveSpeed() => GetSpeed(ReceiveLogger);

    public void EnqueueSend(long size)
    {
        SendLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
    public void EnqueueReceive(long size)
    {
        ReceiveLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }

    public override string ToString()
    {
        return Id.ToString();
    }
}