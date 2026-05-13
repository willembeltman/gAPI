namespace gAPI.Core.ServiceBus.Interfaces;

public interface IServiceBusReceiver
{
    Task StartAsync(string busName, CancellationToken ct);
}