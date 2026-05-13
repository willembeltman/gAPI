using gAPI.Core.Ids;
using gAPI.Fabric.Server.Models;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Collections;

public sealed class FabricHostCollection : IEnumerable<FabricHost>
{
    private long _nextId;
    private readonly ConcurrentDictionary<FabricHostId, FabricHost> Clients = new();

    public FabricHostCollection(FabricManager fabricManager)
    {
        FabricManager = fabricManager;
    }

    public FabricManager FabricManager { get; }

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

    public IEnumerator<FabricHost> GetEnumerator() => Clients.Values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
