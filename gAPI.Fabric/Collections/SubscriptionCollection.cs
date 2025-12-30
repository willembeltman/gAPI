using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public class SubscriptionCollection : IEnumerable<Subscription>
{
    private readonly ConcurrentDictionary<SubscriptionId, Subscription> Subscriptions = new();
    public int Count => Subscriptions.Count;

    public Subscription GetOrCreate(SubscriptionId subscriberId, FabricConnection connection)
    {
        return Subscriptions.GetOrAdd(subscriberId, _ => new Subscription(subscriberId, connection));
    }

    internal Subscription? TryGet(SubscriptionId subscriberId)
    {
        if (!Subscriptions.TryGetValue(subscriberId, out var subscriber))
            return null;
        return subscriber;
    }
    public bool Remove(SubscriptionId subscriberId)
    {
        return Subscriptions.TryRemove(subscriberId, out _);
    }

    public IEnumerator<Subscription> GetEnumerator() => Subscriptions.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}