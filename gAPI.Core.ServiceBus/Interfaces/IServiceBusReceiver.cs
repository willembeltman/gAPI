using gAPI.Core.ServiceBus.Enums;

namespace gAPI.Core.ServiceBus.Interfaces;

public interface IServiceBusReceiver
{
    Task StartAsync(ServiceBusReceiver bus, CancellationToken ct);
}