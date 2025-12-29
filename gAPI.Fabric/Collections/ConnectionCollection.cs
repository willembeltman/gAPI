using gAPI.Fabric.Types;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public sealed class ConnectionCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<ConnectionId, Connection> Clients = new();

    public ConnectionId AddConnection(Connection client)
    {
        var id = new ConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(ConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<Connection> All => Clients.Values;
}
