using gAPI.Core.Ids;

namespace gAPI.Core.Client.Interfaces;

public interface IHostedHubService : IHubServiceSubscription
{
    Task OnStartedAsync(ServiceId serviceId, CancellationToken ct);
    Task OnStoppedAsync(ServiceId serviceId, CancellationToken ct);
}
