using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric;

public class SubscriberRegistry : IEnumerable<Subscriber>
{
    private readonly ConcurrentDictionary<SubscriberId, Subscriber> Subscribers = new();
    public int Count => Subscribers.Count;
    public Subscriber GetOrCreate(SubscriberId subscriberId, BusClient busClient)
    {
        return Subscribers.GetOrAdd(subscriberId, _ => new Subscriber(subscriberId, busClient));
    }
    internal Subscriber? TryGet(SubscriberId subscriberId)
    {
        if (!Subscribers.TryGetValue(subscriberId, out var subscriber))
            return null;
        return subscriber;
    }
    public bool Remove(SubscriberId subscriberId)
    {
        return Subscribers.TryRemove(subscriberId, out _);
    }

    public IEnumerator<Subscriber> GetEnumerator() => Subscribers.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}