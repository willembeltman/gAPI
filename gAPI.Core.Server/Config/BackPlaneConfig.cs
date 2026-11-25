namespace gAPI.Core.Server.Config;

public record BackPlaneConfig(
    int? MaxPackageSize,
    string ConnectionString);