namespace gAPI.Fabric;

public record Subscriber(
    SubscriberId Id,
    BusClient BusClient);