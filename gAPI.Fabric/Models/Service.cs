using gAPI.Fabric.Collections;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Service(ServiceId Id)
{
    public SubscriptionCollection Subscriptions { get; } = new();
    public UserCollection Users { get; } = new();
    public SessionCollection Scopes { get; } = new();

    public void Subscribe(SubscriptionId subscriberId, FabricHost connection)
    {
        var subscriber = Subscriptions.GetOrCreate(subscriberId, connection);
        var user = Users.GetOrCreate(subscriberId.UserId);
        var scope = Scopes.GetOrCreate(subscriberId.SessionId);

        user.Subscribe(subscriberId, connection);
        scope.Subscribe(subscriberId, connection);
    }

    public void UnSubscribe(SubscriptionId subscriberId, FabricHost connection)
    {
        var user = Users.TryGet(subscriberId.UserId);
        var scope = Scopes.TryGet(subscriberId.SessionId);

        scope?.UnSubscribe(subscriberId);
        user?.UnSubscribe(subscriberId);

        Subscriptions.Remove(subscriberId);
        if (scope?.Subscriptions.Count == 0)
            Scopes.Remove(subscriberId.SessionId);
        if (user?.Subscriptions.Count == 0)
            Users.Remove(subscriberId.UserId);
    }

    public void Publish(ServiceId serviceId, string messageData)
    {
        foreach (var connection in Subscriptions
            .GroupBy(a => a.Connection)
            .Select(a => a.Key))
        {
            connection.SendMessage(
                new SseMessage(serviceId, null, null, messageData));
        }
    }
}