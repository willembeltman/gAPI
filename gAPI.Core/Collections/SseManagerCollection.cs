using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Core.Collections;

public sealed class SseManagerCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<SseManagerId, IClientConnection> Clients = new();

    public SseManagerId Add(IClientConnection client)
    {
        var id = new SseManagerId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool Remove(SseManagerId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<IClientConnection> All => Clients.Values;
}
