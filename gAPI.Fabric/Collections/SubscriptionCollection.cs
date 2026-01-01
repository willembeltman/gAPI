using gAPI.FabricClient.Models;
using gAPI.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.FabricClient.Collections;

public class SubscriptionCollection : IEnumerable<Subscription>
{
    private readonly ConcurrentDictionary<SubscriptionId, Subscription> Subscriptions = new();
    public int Count => Subscriptions.Count;

    public Subscription GetOrCreate(SubscriptionId subscriberId, FabricHost connection)
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