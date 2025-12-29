using gAPI.Fabric.Collections;
using gAPI.Fabric.Helpers;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Scope(ScopeId Id) : IPublishable
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public void Subscribe(SubscriptionId subscriberId, Connection connection)
    {
        Subscriptions.GetOrCreate(subscriberId, connection);
    }

    public void UnSubscribe(SubscriptionId subscriberId)
    {
        Subscriptions.Remove(subscriberId);
    }

    public void Publish(ServiceId serviceId, byte[] messageData)
    {
        foreach (var subscriber in Subscriptions)
        {
            subscriber.Connection.SendMessage(serviceId, null, Id, messageData);
        }
    }
}
