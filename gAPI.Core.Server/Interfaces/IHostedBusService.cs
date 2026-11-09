namespace gAPI.Core.Server.Interfaces;

public interface IHostedBusService
{
    Task OnStartedAsync(IBaseBusConnection busConnection, CancellationToken ct);
    Task OnStoppedAsync(IBaseBusConnection busConnection, CancellationToken ct);
    Task OnSubscribedAsync(IBaseBusConnection busConnection, CancellationToken ct);
    Task OnUnsubscribedAsync(IBaseBusConnection busConnection, CancellationToken ct);
}
