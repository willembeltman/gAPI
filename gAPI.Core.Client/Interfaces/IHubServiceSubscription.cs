using gAPI.Core.Ids;

namespace gAPI.Core.Client.Interfaces;

public interface IHubServiceSubscription
{
    Task OnSubscribedAsync(ServiceId serviceId, CancellationToken ct);
    Task OnUnsubscribedAsync(ServiceId serviceId, CancellationToken ct);
}
