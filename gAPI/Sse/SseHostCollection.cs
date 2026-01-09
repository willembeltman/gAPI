using gAPI.Ids;
using System.Collections.Concurrent;

namespace gAPI.Sse;

public sealed class SseHostCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<SseHostId, SseHost> SseHosts = new();

    public SseHostId Add(SseHost client)
    {
        var id = new SseHostId(Interlocked.Increment(ref _nextId));
        SseHosts[id] = client;
        return id;
    }

    public bool Remove(SseHostId id)
    {
        return SseHosts.TryRemove(id, out _);
    }

    public IEnumerable<SseHost> All => SseHosts.Values;
}