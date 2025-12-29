using gAPI.Fabric.Collections;
using gAPI.Fabric.Helpers;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Service(ServiceId Id) : IPublishable
{
    public SubscriptionCollection Subscriptions { get; } = new();
    public ServiceUserCollection Users { get; } = new();
    public ServiceScopeCollection Scopes { get; } = new();

    public void Subscribe(SubscriptionId subscriberId, Connection busClient)
    {
        var subscriber = Subscriptions.GetOrCreate(subscriberId, busClient);
        var user = Users.GetOrCreate(subscriberId.UserId);
        var scope = Scopes.GetOrCreate(subscriberId.ScopeId);
        
        user.Subscribe(subscriberId, busClient);
        scope.Subscribe(subscriberId, busClient);
    }

    public void UnSubscribe(SubscriptionId subscriberId, Connection busClient)
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
}