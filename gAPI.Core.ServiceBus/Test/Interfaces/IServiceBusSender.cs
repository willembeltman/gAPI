using UwvLlm.Api.Core.Enums;

namespace UwvLlm.Infrastructure.Messaging.Interfaces;

public interface IServiceBusSender
{
    Task SendAsync<TMessage>(ServiceBusReceiver bus, TMessage message, CancellationToken ct);
}