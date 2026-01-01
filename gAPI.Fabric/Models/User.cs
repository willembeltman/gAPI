using gAPI.FabricClient.Collections;
using gAPI.Sse;
using gAPI.Types;

namespace gAPI.FabricClient.Models;

public record User(UserId Id)
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public Subscription Subscribe(SubscriptionId subscriberId, FabricHost connection)
    {
        return Subscriptions.GetOrCreate(subscriberId, connection);
    }

    public bool UnSubscribe(SubscriptionId subscriberId)
    {
        return Subscriptions.Remove(subscriberId);
    }

    public void Publish(ServiceId serviceId, string messageData)
    {
        foreach (var connection in Subscriptions
            .GroupBy(a => a.Connection)
            .Select(a => a.Key))
        {
            connection.SendMessage(
                new SseMessage(serviceId, Id, null, messageData));
        }
    }
}
