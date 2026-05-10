namespace gAPI.Core.ServiceBus.Messages;

public record ServiceBusMessage(
    string MessageType,
    string Payload);
