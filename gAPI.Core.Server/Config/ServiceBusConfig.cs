namespace gAPI.Core.Server.Config;

public record ServiceBusConfig(
    int? MaxPackageSize,
    string QueueName,
    string ConnectionString);