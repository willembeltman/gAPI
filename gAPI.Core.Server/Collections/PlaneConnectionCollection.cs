using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class PlaneConnectionCollection
{
    readonly ConcurrentDictionary<PlaneConnectionId, IFabricBasePlaneConnection> PlaneConnections = new();
    public readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<PlaneConnectionId, IFabricBasePlaneConnection>> Services = [];

    public IEnumerable<IFabricBasePlaneConnection> All => PlaneConnections.Values;

    public PlaneConnectionId Add(IFabricBasePlaneConnection client, ServiceId serviceId)
    {
        var id = client.PlaneConnectionId;
        PlaneConnections[id] = client;
        var connections = Services.GetOrAdd(serviceId, (_) => { return new ConcurrentDictionary<PlaneConnectionId, IFabricBasePlaneConnection>(); });
        connections[id] = client;
        return id;
    }

    public bool Remove(PlaneConnectionId id)
    {
        return PlaneConnections.TryRemove(id, out _);
    }

}