using gAPI.Core.Ids;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Models;

public class Service(ServiceId Id, FabricManager FabricManager) : PerformanceCounter, IActor
{
    public ServiceId Id { get; } = Id;
    public FabricManager FabricManager { get; } = FabricManager;
    public ConcurrentDictionary<UserId, UserSubsciption> Users { get; } = new();
    public ConcurrentDictionary<SessionId, SessionSubscription> Sessions { get; } = new();
    public ConcurrentDictionary<FabricConnectionId, FabricHost> Connections { get; } = new();
    public ConcurrentDictionary<BusConnectionId, BusConnection> BusConnections { get; } = new();
    public ConcurrentDictionary<PlaneConnectionId, PlaneConnection> PlaneConnections { get; } = new();
    public ConcurrentDictionary<ServiceSubscriptionId, PlaneServiceSubscription> PlaneServiceSubscriptions { get; } = new();

    public async Task Subscribe(FabricHost connection, UserId? userId, SessionId sessionId, long receiveSize)
    {
        if (userId != null)
        {
            Users.AddOrUpdate(userId,
                (userId) =>
                {
                    var user = new UserSubsciption(userId);
                    user.Subscribe(connection);
                    return user;
                },
                (userId, user) =>
                {
                    user.Subscribe(connection);
                    return user;
                });
        }
        Sessions.AddOrUpdate(sessionId,
            (sessionId) =>
            {
                var session = new SessionSubscription(sessionId);
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
    public async Task Unsubscribe(FabricHost connection, UserId? userId, SessionId sessionId, long receiveSize)
    {
        if (userId != null)
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
    public async Task PlaneSubscribe(FabricPlaneHost connection, PlaneConnectionId planeConnectionId, ServiceSubscriptionId serviceSubscriptionId, long receiveSize)
    {
        PlaneConnections.AddOrUpdate(planeConnectionId,
            (planeConnectionId) => new PlaneConnection(planeConnectionId, connection),
            (planeConnectionId, planeConnection) => planeConnection);
        PlaneServiceSubscriptions.AddOrUpdate(serviceSubscriptionId,
            (serviceSubscriptionId) => new PlaneServiceSubscription(serviceSubscriptionId, connection),
            (serviceSubscriptionId, planeConnection) => planeConnection);
        EnqueueReceive(receiveSize);
    }
    public async Task PlaneUnsubscribe(FabricPlaneHost connection, PlaneConnectionId planeConnectionId, ServiceSubscriptionId serviceSubscriptionId, long receiveSize)
    {
        PlaneConnections.TryRemove(planeConnectionId, out _);
        PlaneServiceSubscriptions.TryRemove(serviceSubscriptionId, out _);
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

    internal void RemovePlaneConnection(FabricPlaneHost host)
    {
        var planeConnection = PlaneConnections.Values
            .FirstOrDefault(a => a.Connection.FabricPlaneConnectionId == host.FabricPlaneConnectionId);
        if (planeConnection == null)
        {
            return;
        }
        PlaneConnections.TryRemove(planeConnection.PlaneConnectionId, out _);
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

    public override string ToString()
    {
        return Id.ToString();
    }
}