using gAPI.Fabric.Collections;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Session(SessionId Id)
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public void Subscribe(SubscriptionId subscriberId, FabricConnection connection)
    {
        Subscriptions.GetOrCreate(subscriberId, connection);
    }

    public void UnSubscribe(SubscriptionId subscriberId)
    {
        Subscriptions.Remove(subscriberId);
    }

    public void Publish(ServiceId serviceId, string messageData)
    {
        foreach (var subscriber in Subscriptions)
            subscriber.Connection.SendMessage(
                new SendMessage(serviceId, null, Id, messageData));
    }
}
