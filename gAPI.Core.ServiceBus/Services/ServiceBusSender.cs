using gAPI.Core.ServiceBus.Interfaces;
using gAPI.Core.ServiceBus.Messages;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace gAPI.Core.ServiceBus.Services;

public class ServiceBusSender(
    IRabbitConnectionProvider provider) 
    : IServiceBusSender
{
    public async Task SendAsync<TMessage>(Enums.ServiceBusReceiver bus, TMessage message, CancellationToken ct)
    {
        var connection = await provider.GetConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var envelope = new ServiceBusMessage(
            MessageType: typeof(TMessage).FullName!,
            Payload: JsonSerializer.Serialize(message)
        );

        var json = JsonSerializer.Serialize(envelope);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: Enum.GetName(bus)!,
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true },
            body: body,
            cancellationToken: ct
        );
    }
}