using gAPI.Fabric.Types;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public sealed class FabricConnectionCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<FabricConnectionId, FabricConnection> Clients = new();

    public FabricConnectionId AddConnection(FabricConnection client)
    {
        var id = new FabricConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(FabricConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<FabricConnection> All => Clients.Values;
}
