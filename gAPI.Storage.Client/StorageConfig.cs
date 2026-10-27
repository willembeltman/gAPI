namespace gAPI.Storage.Client;

public record StorageConfig(
    int? MaxPackageSize = null,
    string? StorageConnectionString = null
);