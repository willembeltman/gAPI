using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IPlaneServiceSubscription
{
    Task OnSubscribedAsync(ServiceId serviceId, CancellationToken ct);
    Task OnUnsubscribedAsync(ServiceId serviceId, CancellationToken ct);
}
