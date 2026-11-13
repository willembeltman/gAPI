using gAPI.Core.Enums;
using gAPI.Core.Ids;
using gAPI.Core.Server.Entities;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace gAPI.Fabric.Server.Models;

public record Service(ServiceId Id, FabricManager FabricManager) : IActor
{
    public ServiceId Id { get; } = Id;
    public ConcurrentDictionary<UserId, User> Users { get; } = new();
    public ConcurrentDictionary<SessionId, Session> Sessions { get; } = new();
    public ConcurrentDictionary<FabricConnectionId, FabricHost> Connections { get; } = new();
    public ConcurrentDictionary<BusConnectionId, BusConnection> BusConnections { get; } = new();

    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    private readonly ConcurrentQueue<(double time, long bytes)> SendLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceiveLogger = new();


    public async Task Subscribe(FabricHost connection, UserId userId, SessionId sessionId, long receiveSize)
    {
        Users.AddOrUpdate(userId,
            (userId) =>
            {
                var user = new User(userId);
                user.Subscribe(connection);
                return user;
            },
            (userId, user) =>
            {
                user.Subscribe(connection);
                return user;
            });
        Sessions.AddOrUpdate(sessionId,
            (sessionId) =>
            {
                var session = new Session(sessionId);
                session.Subscribe(connection);
                return session;
            },
            (sessionId, session) =>
            {
                session.Subscribe(connection);
                return session;
            });
        Connections.TryAdd(connection.FabricConnectionId, connection);
        EnqueueReceive(receiveSize);
    }
    public async Task Unsubscribe(FabricHost connection, UserId userId, SessionId sessionId, long receiveSize)
    {
        Users.TryRemove(userId, out _);
        Sessions.TryRemove(sessionId, out _);
        Connections.TryRemove(connection.FabricConnectionId, out _);
        EnqueueReceive(receiveSize);
    }
    public async Task BusSubscribe(FabricBusHost connection, BusConnectionId busConnectionId, long receiveSize)
    {
        BusConnections.AddOrUpdate(busConnectionId,
            (busConnectionId) => new BusConnection(busConnectionId, connection),
            (busConnectionId, busConnection) => busConnection);
        EnqueueReceive(receiveSize);
    }
    public async Task BusUnsubscribe(FabricBusHost connection, BusConnectionId busConnectionId, long receiveSize)
    {
        BusConnections.TryRemove(busConnectionId, out _);
        EnqueueReceive(receiveSize);
    }

    internal void RemoveConnection(FabricHost host)
    {
        foreach (var user in Users.Values)
        {
            user.RemoveHost(host);
        }
        foreach (var session in Sessions.Values)
        {
            session.RemoveHost(host);
        }
        Connections.TryRemove(host.FabricConnectionId, out _);
    }

    internal void RemoveBusConnection(FabricBusHost host)
    {
        var busConnection = BusConnections.Values
            .FirstOrDefault(a => a.Connection.FabricBusConnectionId == host.FabricBusConnectionId);
        if (busConnection == null)
        {
            return;
        }
        BusConnections.TryRemove(busConnection.BusConnectionId, out _);
    }

    public (IEnumerable<FabricHost>, IActor) GetFabricHosts(UserId? userId, SessionId? sessionId)
    {
        if (userId != null)
        {
            
            if (Users.TryGetValue(userId, out var user) == false) 
                return ([], this);
            return (user.Connections.Values, user);
        }
        else if (sessionId != null)
        {
            if (Sessions.TryGetValue(sessionId, out var session) == false) 
                return ([], this);
            return (session.Connections.Values, session);
        }
        else
        {
            return (Connections.Values, this);
        }
    }
    //public (IEnumerable<FabricBusHost>, IActor) GetFabricBusHosts(BusToChoice To, BusConnectionId? ToBusConnectionId)
    //{
    //    if (To == BusToChoice.ToAny)
    //    {
    //        var busConnection = BusConnections.Values.FirstOrDefault(); // TODO iets met loadbalancing doen
    //        if (busConnection == null) return ([], this);
    //        return ([busConnection.Connection], busConnection);
    //    }
    //    else if (To == BusToChoice.ToBusConnectionId && ToBusConnectionId != null)
    //    {
    //        if (BusConnections.TryGetValue(ToBusConnectionId, out var busConnection) == false)
    //            return ([], this);
    //        return ([busConnection.Connection], busConnection);
    //    }
    //    else
    //    {
    //        return (BusConnections.Values.Select(a => a.Connection), this);
    //    }
    //}

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
        return new((int)(count / interval), (int)(bytes / interval));
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