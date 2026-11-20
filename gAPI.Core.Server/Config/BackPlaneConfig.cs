namespace gAPI.Core.Server.Config;

public record BackPlaneConfig(
    int? MaxPackageSize,
    string QueueName,
    string ConnectionString);