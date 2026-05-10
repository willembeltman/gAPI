using UwvLlm.Api.Core.Enums;

namespace UwvLlm.Infrastructure.Messaging.Interfaces;

public interface IServiceBusReceiver
{
    Task StartAsync(ServiceBusReceiver bus, CancellationToken ct);
}