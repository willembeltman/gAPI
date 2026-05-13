namespace gAPI.Core.ServiceBus.Interfaces;

public interface IServiceBusSender
{
    Task SendAsync<TMessage>(string bus, TMessage message, CancellationToken ct);
}