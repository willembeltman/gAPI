using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IBusServiceSubscription
{
    Task OnSubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct);
    Task OnUnsubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct);
}
