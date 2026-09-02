using gAPI.Core.Ids;
using gAPI.Core.Server.Wss;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class WssServerConnectionCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<ClientConnectionId, WssServerConnection> Clients = new();

    public ClientConnectionId AddConnection(WssServerConnection client)
    {
        var id = new ClientConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(ClientConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<WssServerConnection> All => Clients.Values;
}
