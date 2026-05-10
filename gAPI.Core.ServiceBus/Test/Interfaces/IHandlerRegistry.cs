using UwvLlm.Infrastructure.Messaging.Messages;

namespace UwvLlm.Infrastructure.Messaging.Interfaces;

public interface IHandlerRegistry
{
    Task Handle(ServiceBusMessage message, IServiceProvider sp, CancellationToken ct);
}