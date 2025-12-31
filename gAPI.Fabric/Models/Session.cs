using gAPI.Fabric.Collections;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Session(SessionId Id)
{
    public SubscriptionCollection Subscriptions { get; } = new();

    public void Subscribe(SubscriptionId subscriberId, FabricHost connection)
    {
        Subscriptions.GetOrCreate(subscriberId, connection);
    }

    public void UnSubscribe(SubscriptionId subscriberId)
    {
        Subscriptions.Remove(subscriberId);
    }

    public void Publish(ServiceId serviceId, string messageData)
    {
        foreach (var connection in Subscriptions
            .GroupBy(a => a.Connection)
            .Select(a => a.Key))
        {
            connection.SendMessage(
                new SseMessage(serviceId, null, Id, messageData));
        }
    }
}
