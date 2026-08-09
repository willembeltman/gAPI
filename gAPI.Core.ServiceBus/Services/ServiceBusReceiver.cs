using gAPI.Core.ServiceBus.Interfaces;
using gAPI.Core.ServiceBus.Messages;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace gAPI.Core.ServiceBus.Services;

public class ServiceBusReceiver(
    IRabbitServiceBusConnectionProvider provider,
    IServiceScopeFactory scopeFactory,
    IConsoleService console)
    : IServiceBusReceiver
{
    public async Task StartAsync(string busName, CancellationToken ct)
    {
        var connection = await provider.GetConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            busName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, e) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var json = Encoding.UTF8.GetString(e.Body.ToArray());

            try
            {
                var message = JsonSerializer.Deserialize<ServiceBusMessage>(json)
                    ?? throw new Exception("Invalid message");

                var registry = scope.ServiceProvider.GetRequiredService<IServiceBusHandlerRegistry>();

                await registry.Handle(message, scope.ServiceProvider, ct);

                await channel.BasicAckAsync(e.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                console.WriteLine(ex);
            }
        };

        await channel.BasicConsumeAsync(busName!, false, consumer);
    }
}
