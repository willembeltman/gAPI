using gAPI.Fabric.Collections;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record User(UserId Id)
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public Subscription Subscribe(SubscriptionId subscriberId, FabricConnection connection)
    {
        return Subscriptions.GetOrCreate(subscriberId, connection);
    }

    public bool UnSubscribe(SubscriptionId subscriberId)
    {
        return Subscriptions.Remove(subscriberId);
    }

    public void Publish(ServiceId serviceId, string messageData)
    {
        foreach (var subscriber in Subscriptions)
            subscriber.Connection.SendMessage(
                new SendMessage(serviceId, Id, null, messageData));
    }
}
