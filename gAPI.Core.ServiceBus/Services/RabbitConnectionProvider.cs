using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using gAPI.Core.ServiceBus.Interfaces;

namespace gAPI.Core.ServiceBus.Services;

public class RabbitConnectionProvider : IRabbitConnectionProvider
{
    private readonly IConfiguration _config;
    private IConnection? _connection;

    public RabbitConnectionProvider(IConfiguration config)
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