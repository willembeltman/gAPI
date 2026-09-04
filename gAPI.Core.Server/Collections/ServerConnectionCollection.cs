using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class ServerConnectionCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<ClientConnectionId, IServerConnection> Clients = new();

    public ClientConnectionId AddConnection(IServerConnection client)
    {
        var id = new ClientConnectionId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool RemoveConnection(ClientConnectionId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<IServerConnection> All => Clients.Values;
}
