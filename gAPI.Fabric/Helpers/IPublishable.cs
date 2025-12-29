using gAPI.Fabric.Collections;

namespace gAPI.Fabric.Helpers;

public interface IPublishable
{
    SubscriptionCollection Subscriptions { get; }
}
