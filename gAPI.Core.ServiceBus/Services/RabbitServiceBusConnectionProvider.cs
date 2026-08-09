using gAPI.Core.ServiceBus.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace gAPI.Core.ServiceBus.Services;

public class RabbitServiceBusConnectionProvider : IRabbitServiceBusConnectionProvider
{
    private readonly IConfiguration _config;
    private IConnection? _connection;

    public RabbitServiceBusConnectionProvider(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IConnection> GetConnectionAsync()
    {
        if (_connection != null && _connection.IsOpen)
            return _connection;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.GetConnectionString("rabbit")!)
        };

        _connection = await factory.CreateConnectionAsync();
        return _connection;
    }
}