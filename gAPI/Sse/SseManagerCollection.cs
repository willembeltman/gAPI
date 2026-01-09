using gAPI.Ids;
using gAPI.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace gAPI.Sse;


public sealed class SseManagerCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<SseManagerId, ISseManagerBase> Clients = new();

    public SseManagerId Add(ISseManagerBase client)
    {
        var id = new SseManagerId(Interlocked.Increment(ref _nextId));
        Clients[id] = client;
        return id;
    }

    public bool Remove(SseManagerId id)
    {
        return Clients.TryRemove(id, out _);
    }

    public IEnumerable<ISseManagerBase> All => Clients.Values;
}