using gAPI.Fabric.Collections;
using gAPI.Fabric.Helpers;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Service(ServiceId Id) : IPublishable
{
    public SubscriptionCollection Subscriptions { get; } = new();
    public UserCollection Users { get; } = new();
    public ScopeCollection Scopes { get; } = new();

    public void Subscribe(SubscriptionId subscriberId, Connection connection)
    {
        var subscriber = Subscriptions.GetOrCreate(subscriberId, connection);
        var user = Users.GetOrCreate(subscriberId.UserId);
        var scope = Scopes.GetOrCreate(subscriberId.ScopeId);

        user.Subscribe(subscriberId, connection);
        scope.Subscribe(subscriberId, connection);
    }

    public void UnSubscribe(SubscriptionId subscriberId, Connection connection)
    {
        var user = Users.TryGet(subscriberId.UserId);
        var scope = Scopes.TryGet(subscriberId.ScopeId);

        scope?.UnSubscribe(subscriberId);
        user?.UnSubscribe(subscriberId);

        Subscriptions.Remove(subscriberId);
        if (scope?.Subscriptions.Count == 0)
            Scopes.Remove(subscriberId.ScopeId);
        if (user?.Subscriptions.Count == 0)
            Users.Remove(subscriberId.UserId);
    }

    public void Publish(ServiceId serviceId, byte[] messageData)
    {
        foreach (var subscriber in Subscriptions)
        {
            subscriber.Connection.SendMessage(serviceId, null, null, messageData);
        }
    }
}