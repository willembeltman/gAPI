using RabbitMQ.Client;

namespace gAPI.Core.ServiceBus.Interfaces;

public interface IRabbitServiceBusConnectionProvider
{
    Task<IConnection> GetConnectionAsync();
}