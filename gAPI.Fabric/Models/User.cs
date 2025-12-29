using gAPI.Fabric.Collections;
using gAPI.Fabric.Helpers;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record User(UserId Id) : IPublishable
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public Subscription Subscribe(SubscriptionId subscriberId, Connection connection)
    {
        return Subscriptions.GetOrCreate(subscriberId, connection);
    }

    public bool UnSubscribe(SubscriptionId subscriberId)
    {
        return Subscriptions.Remove(subscriberId);
    }

    public void Publish(ServiceId serviceId, byte[] messageData)
    {
        foreach (var subscriber in Subscriptions)
        {
            subscriber.Connection.SendMessage(serviceId, Id, null, messageData);
        }
    }
}
