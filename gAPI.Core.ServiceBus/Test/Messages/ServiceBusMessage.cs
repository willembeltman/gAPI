namespace UwvLlm.Infrastructure.Messaging.Messages;

public record ServiceBusMessage(
    string MessageType,
    string Payload);
