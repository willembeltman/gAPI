using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IPlaneServiceSubscription
{
    Task OnSubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct);
    Task OnUnsubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct);
}
