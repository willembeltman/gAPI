using gAPI.Core.Ids;

namespace gAPI.Core.Client.Interfaces;

public interface IHubServiceSubscription
{
    Task OnSubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct);
    Task OnUnsubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct);
}
