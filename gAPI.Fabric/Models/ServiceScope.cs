using gAPI.Fabric.Collections;
using gAPI.Fabric.Helpers;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record ServiceScope(ScopeId Id) : IPublishable
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public void Subscribe(SubscriptionId subscriberId, Connection busClient)
    {
        Subscriptions.GetOrCreate(subscriberId, busClient);
    }

    public void UnSubscribe(SubscriptionId subscriberId)
    {
        Subscriptions.Remove(subscriberId);
    }
}
