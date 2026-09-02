using gAPI.Core.Ids;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Collections;

public sealed class FabricHostCollection : IEnumerable<FabricHost>
{
    private long _nextId;
    private readonly ConcurrentDictionary<FabricConnectionId, FabricHost> Clients = new();

    public FabricConnectionId AddConnection(FabricHost client)
    {
        var id = new FabricConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(FabricConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerator<FabricHost> GetEnumerator() => Clients.Values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
