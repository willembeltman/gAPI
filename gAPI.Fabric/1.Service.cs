
namespace gAPI.Fabric;

public record Service(ServiceId Id) : IPublishable
{
    public SubscriberRegistry AllSubscribers { get; } = new();
    public UserRegistry Users { get; } = new();
    public ScopeRegistry Scopes { get; } = new();

    public void Subscribe(SubscriberId subscriberId, BusClient busClient)
    {
        var subscriber = AllSubscribers.GetOrCreate(subscriberId, busClient);
        var user = Users.GetOrCreate(subscriberId.UserId);
        var scope = Scopes.GetOrCreate(subscriberId.ScopeId);
        
        user.Subscribe(subscriberId, busClient);
        scope.Subscribe(subscriberId, busClient);
    }

    public void UnSubscribe(SubscriberId subscriberId, BusClient busClient)
    {
        var user = Users.TryGet(subscriberId.UserId);
        var scope = Scopes.TryGet(subscriberId.ScopeId);

        scope?.UnSubscribe(subscriberId);
        user?.UnSubscribe(subscriberId);

        AllSubscribers.Remove(subscriberId);
        if (scope?.AllSubscribers.Count == 0)
            Scopes.Remove(subscriberId.ScopeId);
        if (user?.AllSubscribers.Count == 0)
            Users.Remove(subscriberId.UserId);
    }
}