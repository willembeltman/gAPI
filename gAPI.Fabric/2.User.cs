
namespace gAPI.Fabric;

public record User(UserId Id) : IPublishable
{
    public SubscriberRegistry AllSubscribers { get; } = new();

    public Subscriber Subscribe(SubscriberId subscriberId, BusClient busClient)
    {
        return AllSubscribers.GetOrCreate(subscriberId, busClient);
    }

    public bool UnSubscribe(SubscriberId subscriberId)
    {
        return AllSubscribers.Remove(subscriberId);
    }
}
