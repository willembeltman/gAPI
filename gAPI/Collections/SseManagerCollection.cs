using gAPI.Ids;
using gAPI.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Collections;

public sealed class SseManagerCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<SseManagerId, ISseClientConnection> Clients = new();

    public SseManagerId Add(ISseClientConnection client)
    {
        var id = new SseManagerId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool Remove(SseManagerId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<ISseClientConnection> All => Clients.Values;
}
