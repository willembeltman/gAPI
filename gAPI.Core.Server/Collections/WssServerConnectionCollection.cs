using gAPI.Core.Ids;
using gAPI.Core.Server.Wss;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class WssServerConnectionCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<ConnectionId, WssServerConnection> Clients = new();

    public ConnectionId AddConnection(WssServerConnection client)
    {
        var id = new ConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(ConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<WssServerConnection> All => Clients.Values;
}
