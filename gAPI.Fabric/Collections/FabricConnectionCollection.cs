using gAPI.Fabric;
using System.Collections.Concurrent;

namespace gAPI.FabricClient.Collections;

public sealed class FabricHostCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<FabricHostId, FabricHost> Clients = new();

    public FabricHostId AddConnection(FabricHost client)
    {
        var id = new FabricHostId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(FabricHostId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<FabricHost> All => Clients.Values;
}
