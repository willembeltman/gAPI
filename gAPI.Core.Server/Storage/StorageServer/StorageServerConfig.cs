using gAPI.Storage.StorageServer.Dtos;

#nullable enable
namespace gAPI.Storage.StorageServer;

public class StorageServerConfig
{
    public double AuthenticateTimeoutMinutes { get; set; } = 15;
    public double UrlTimeoutSeconds { get; set; } = 5;
    public string? ServerUrl { get; set; }
    public Credential Credential { get; set; } = new Credential();
}