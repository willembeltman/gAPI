using System.Collections.Concurrent;

namespace gAPI.Fabric;

public sealed class ConnectionRegistry
{
    private long _nextId;
    private readonly ConcurrentDictionary<ConnectionId, BusClient> Clients = new();

    public ConnectionId Add(BusClient client)
    {
        var id = new ConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool Remove(ConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<BusClient> All => Clients.Values;
}
