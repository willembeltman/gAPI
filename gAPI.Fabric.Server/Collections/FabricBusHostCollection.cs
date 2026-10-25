using gAPI.Core.Ids;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Collections;

public sealed class FabricBusHostCollection : IEnumerable<FabricBusHost>
{
    private long _nextId;
    private readonly ConcurrentDictionary<FabricBusConnectionId, FabricBusHost> Clients = new();

    public FabricBusConnectionId AddConnection(FabricBusHost client)
    {
        var id = new FabricBusConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(FabricBusConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerator<FabricBusHost> GetEnumerator() => Clients.Values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
