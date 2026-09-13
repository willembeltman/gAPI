using gAPI.Core.Client.Interfaces;
using gAPI.Core.Client.Sse;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Client.Collections;

public sealed class SseManagerCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<SseManagerId, SseClientConnection> Clients = new();

    public SseManagerId Add(SseClientConnection client)
    {
        var id = new SseManagerId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool Remove(SseManagerId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<SseClientConnection> All => Clients.Values;
}
