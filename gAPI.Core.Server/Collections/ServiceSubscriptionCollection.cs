using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class ServiceSubscriptionCollection
{
    long _nextId;
    readonly ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription> ServiceSubscriptions = new();
    public readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>> Services = [];

    public IEnumerable<IServiceSubscription> All => ServiceSubscriptions.Values;

    public void Add(IServiceSubscription client, ServiceId serviceId)
    {
        ServiceSubscriptions[client.ServiceSubscriptionId] = client;
        var connections = Services.GetOrAdd(serviceId, (_) => { return new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(); });
        connections[client.ServiceSubscriptionId] = client;
    }

    public bool Remove(ServiceSubscriptionId id)
    {
        return ServiceSubscriptions.TryRemove(id, out _);
    }

}