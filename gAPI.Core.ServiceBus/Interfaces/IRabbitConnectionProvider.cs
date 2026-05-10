using RabbitMQ.Client;

namespace gAPI.Core.ServiceBus.Interfaces;

public interface IRabbitConnectionProvider
{
    Task<IConnection> GetConnectionAsync();
}