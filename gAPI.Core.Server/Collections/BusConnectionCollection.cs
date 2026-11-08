using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class BusConnectionCollection
{
    readonly ConcurrentDictionary<BusConnectionId, IBusConnection> BusConnections = new();
    public readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<BusConnectionId, IBusConnection>> Services = [];

    public IEnumerable<IBusConnection> All => BusConnections.Values;

    public BusConnectionId Add(IBusConnection client, ServiceId serviceId)
    {
        var id = client.BusConnectionId;
        BusConnections[id] = client;
        var connections = Services.GetOrAdd(serviceId, (_) => { return new ConcurrentDictionary<BusConnectionId, IBusConnection>(); });
        connections[id] = client;
        return id;
    }

    public bool Remove(BusConnectionId id)
    {
        return BusConnections.TryRemove(id, out _);
    }

}