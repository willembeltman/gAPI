using RabbitMQ.Client;

namespace UwvLlm.Infrastructure.Messaging.Interfaces;

public interface IRabbitConnectionProvider
{
    Task<IConnection> GetConnectionAsync();
}