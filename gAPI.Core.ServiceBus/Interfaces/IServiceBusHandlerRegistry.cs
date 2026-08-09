using gAPI.Core.ServiceBus.Messages;

namespace gAPI.Core.ServiceBus.Interfaces;

public interface IServiceBusHandlerRegistry
{
    Task Handle(ServiceBusMessage message, IServiceProvider sp, CancellationToken ct);
}