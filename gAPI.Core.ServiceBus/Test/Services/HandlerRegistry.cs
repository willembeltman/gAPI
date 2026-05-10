using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using UwvLlm.Infrastructure.Messaging.Interfaces;
using UwvLlm.Infrastructure.Messaging.Messages;

namespace UwvLlm.Infrastructure.Messaging.Services;

public class HandlerRegistry : IHandlerRegistry
{
    private readonly Dictionary<string, (Type handlerType, Type messageType)> _handlers;

    public HandlerRegistry()
    {
        _handlers = new();

        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>)));

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType
                .GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<>));

            var messageType = interfaceType.GetGenericArguments()[0];
            var messageTypeName = messageType.FullName!;

            _handlers[messageTypeName] = (handlerType, messageType);
        }
    }

    public async Task Handle(ServiceBusMessage message, IServiceProvider sp, CancellationToken ct)
    {
        if (!_handlers.TryGetValue(message.MessageType, out var entry))
            throw new Exception($"No handler for {message.MessageType}");

        var (handlerType, messageType) = entry;

        var handler = sp.GetRequiredService(handlerType);

        var typedMessage = JsonSerializer.Deserialize(message.Payload, messageType)
            ?? throw new Exception("Deserialization failed");

        var method = handlerType.GetMethod("Handle")!;
        var task = (Task)method.Invoke(handler, new[] { typedMessage, ct })!;
        await task;
    }
}