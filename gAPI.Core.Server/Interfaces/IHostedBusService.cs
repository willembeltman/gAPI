using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IHostedBusService : IBusServiceSubscription
{
    Task OnStartedAsync(ServiceId serviceId, CancellationToken ct);
    Task OnStoppedAsync(ServiceId serviceId, CancellationToken ct);
}
