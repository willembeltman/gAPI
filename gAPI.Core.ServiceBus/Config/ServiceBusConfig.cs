namespace gAPI.Core.ServiceBus.Config;

public record ServiceBusConfig(
    int? MaxPackageSize,
    string QueueName,
    string ConnectionString);