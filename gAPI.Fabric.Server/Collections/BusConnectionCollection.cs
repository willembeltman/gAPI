using gAPI.Core.Ids;
using gAPI.Fabric.Server.Models;
using gAPI.Fabric.Server.Services;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Collections;

public class BusConnectionCollection : IEnumerable<BusConnection>
{
    private readonly ConcurrentDictionary<BusConnectionId, BusConnection> BusConnections = new();

    public BusConnection? TryGet(BusConnectionId busConnectionId)
    {
        if (!BusConnections.TryGetValue(busConnectionId, out var item))
            return null;
        return item;
    }
    public BusConnection TryAdd(BusConnectionId busConnectionId, FabricHost connection)
    {
        return BusConnections.GetOrAdd(busConnectionId, busConnectionId => new BusConnection(busConnectionId, connection));
    }
    public BusConnection? TryRemove(BusConnectionId busConnectionId, FabricHost connection)
    {
        if (BusConnections.TryRemove(busConnectionId, out var busConnection) == false)
            return null;
        return busConnection;
    }

    public int Count => BusConnections.Count;

    public IEnumerator<BusConnection> GetEnumerator()
    {
        return BusConnections.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}