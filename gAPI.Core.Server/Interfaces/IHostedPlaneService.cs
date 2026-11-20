using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IHostedPlaneService : IPlaneServiceSubscription
{
    Task OnStartedAsync(ServiceId serviceId, CancellationToken ct);
    Task OnStoppedAsync(ServiceId serviceId, CancellationToken ct);
}
