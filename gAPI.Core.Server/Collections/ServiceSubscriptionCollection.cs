using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class ServiceSubscriptionCollection
{
    private long _nextId;
    private readonly ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription> SseServiceSubscriptions = new();

    public ServiceSubscriptionId Add(IServiceSubscription client)
    {
        var id = new ServiceSubscriptionId(Interlocked.Increment(ref _nextId));
        SseServiceSubscriptions[id] = client;
        return id;
    }

    public bool Remove(ServiceSubscriptionId id)
    {
        return SseServiceSubscriptions.TryRemove(id, out _);
    }

    public IEnumerable<IServiceSubscription> All => SseServiceSubscriptions.Values;
}