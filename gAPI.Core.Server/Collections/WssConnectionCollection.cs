using gAPI.Wss;
using gAPI.Ids;
using System.Collections.Concurrent;

namespace gAPI.Collections;

public sealed class WssConnectionCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<ConnectionId, WssConnection> Clients = new();

    public ConnectionId AddConnection(WssConnection client)
    {
        var id = new ConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(ConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<WssConnection> All => Clients.Values;
}
