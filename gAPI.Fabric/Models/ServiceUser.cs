using gAPI.Fabric.Collections;
using gAPI.Fabric.Helpers;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record ServiceUser(UserId Id) : IPublishable
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public Subscription Subscribe(SubscriptionId subscriberId, Connection busClient)
    {
        return Subscriptions.GetOrCreate(subscriberId, busClient);
    }

    public bool UnSubscribe(SubscriptionId subscriberId)
    {
        return Subscriptions.Remove(subscriberId);
    }
}
