using gAPI.Core.ServiceBus.Enums;

namespace gAPI.Core.ServiceBus.Interfaces;

public interface IServiceBusSender
{
    Task SendAsync<TMessage>(ServiceBusReceiver bus, TMessage message, CancellationToken ct);
}