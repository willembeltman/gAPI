namespace gAPI.Fabric;

public record Scope(ScopeId Id) : IPublishable
{
    public SubscriberRegistry AllSubscribers { get; } = new();

    public void Subscribe(SubscriberId subscriberId, BusClient busClient)
    {
        AllSubscribers.GetOrCreate(subscriberId, busClient);
    }

    public void UnSubscribe(SubscriberId subscriberId)
    {
        AllSubscribers.Remove(subscriberId);
    }
}
