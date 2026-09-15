using gAPI.Core.ServiceBus.Interfaces;
using gAPI.Core.ServiceBus.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace gAPI.Core.ServiceBus.Services;

public class ServiceBusReceiver(
    ServiceBusConfig config,
    IRabbitServiceBusConnectionProvider provider,
    IServiceScopeFactory scopeFactory,
    IConsoleService console)
    : IHostedService
{
    private CancellationTokenSource? Cts;

    public async Task StartAsync(CancellationToken parentCt)
    {
        Cts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);

        var ct = Cts.Token;
        var queueName = config.QueueName;
        var connection = await provider.GetConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queueName,
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

        await channel.BasicConsumeAsync(queueName!, false, consumer);
    }

    public async Task StopAsync(CancellationToken parentCt)
    {
        if (Cts != null)
        {
            await Cts.CancelAsync();
            Cts.Dispose();
        }
    }

}
