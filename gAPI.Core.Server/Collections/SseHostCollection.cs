using gAPI.Ids;
using gAPI.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Collections;

public sealed class SseHostCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<SseHostId, ISseHost> SseHosts = new();

    public SseHostId Add(ISseHost client)
    {
        var id = new SseHostId(Interlocked.Increment(ref _nextId));
        SseHosts[id] = client;
        return id;
    }

    public bool Remove(SseHostId id)
    {
        return SseHosts.TryRemove(id, out _);
    }

    public IEnumerable<ISseHost> All => SseHosts.Values;
}