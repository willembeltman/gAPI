using gAPI.Core.Ids;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Collections;

public sealed class FabricPlaneHostCollection : IEnumerable<FabricPlaneHost>
{
    private long _nextId;
    private readonly ConcurrentDictionary<FabricPlaneConnectionId, FabricPlaneHost> Clients = new();

    public FabricPlaneConnectionId AddConnection(FabricPlaneHost client)
    {
        var id = new FabricPlaneConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(FabricPlaneConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerator<FabricPlaneHost> GetEnumerator() => Clients.Values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
