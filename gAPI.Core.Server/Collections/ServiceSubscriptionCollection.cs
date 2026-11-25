using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class ServiceSubscriptionCollection
{
    readonly ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription> ServiceSubscriptions = new();
    public readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>> Services = [];

    public IEnumerable<IServiceSubscription> All => ServiceSubscriptions.Values;

    public void Add(IServiceSubscription client, ServiceId serviceId)
    {
        ServiceSubscriptions[client.Id] = client;
        var connections = Services.GetOrAdd(serviceId, (_) => { return new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(); });
        connections[client.Id] = client;
    }

    public bool Remove(ServiceSubscriptionId id, ServiceId serviceId)
    {
        var res = ServiceSubscriptions.TryRemove(id, out _);
        var connections = Services.GetOrAdd(serviceId, (_) => { return new ConcurrentDictionary<ServiceSubscriptionId, IServiceSubscription>(); });
        if (connections.TryRemove(id, out _) == false)
        {
            res = false;
        }
        return res;
    }

}