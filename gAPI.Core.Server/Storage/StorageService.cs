using gAPI.Storage.AzureStorage;
using gAPI.Storage.Mock;
using gAPI.Storage.StorageServer;
using gAPI.Storage.StorageServer.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace gAPI.Storage;

public class StorageService : IStorageService
{
    private readonly IStorageService Implementation;

    public StorageService(
        IConfiguration configuration,
        TimeProvider dateTime) :
        this(configuration.GetConnectionString("StorageConnection"), dateTime)
    {
    }
    public StorageService(
        string? connectionString,
        TimeProvider dateTime)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("StorageConnection ConnectionString is required");

        // Parse connection string
        var parts = connectionString!.Split(';')
            .Where(x => x.Contains('='))
            .Select(x => x.Split(['='], 2))
            .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("Provider", out var provider))
            throw new Exception("ConnectionString must contain 'Provider' parameter");

        Implementation = provider.ToLower() switch
        {
            "mock" => CreateMockService(parts),
            "azure" => CreateAzureService(parts),
            "storageserver" => CreateStorageServerService(parts, dateTime),
            _ => throw new Exception($"Unknown storage provider: {provider}"),
        };
    }

    private IStorageService CreateMockService(Dictionary<string, string> parts)
    {
        var mockConfig = new MockStorageConfig();

        if (parts.TryGetValue("BaseUrl", out var baseUrl))
            mockConfig.BaseUrl = baseUrl;

        if (parts.TryGetValue("LatencyMs", out var latencyStr) && int.TryParse(latencyStr, out var latency))
            mockConfig.SimulateLatencyMs = latency;

        return new MockStorageService(Options.Create(mockConfig));
    }
    private IStorageService CreateAzureService(Dictionary<string, string> parts)
    {
        var remoteConfig = new AzureStorageConfig();

        if (parts.TryGetValue("ContainerName", out var containerName))
            remoteConfig.ContainerName = containerName;

        remoteConfig.ConnectionString = string.Join(";", parts
            .Where(kv => !kv.Key.Equals("Provider", StringComparison.OrdinalIgnoreCase) &&
                         !kv.Key.Equals("ContainerName", StringComparison.OrdinalIgnoreCase))
            .Select(kv => $"{kv.Key}={kv.Value}"));

        return new AzureStorageService(Options.Create(remoteConfig));
    }
    private IStorageService CreateStorageServerService(Dictionary<string, string> parts, TimeProvider dateTime)
    {
        var remoteConfig = new StorageServerConfig();

        if (parts.TryGetValue("UrlTimeout", out var urlTimeoutString)) 
            if (int.TryParse(urlTimeoutString, out var urlTimeout))
                remoteConfig.UrlTimeoutSeconds = urlTimeout;

        if (parts.TryGetValue("AuthenticateTimeout", out var AuthenticateTimeoutString))
            if (int.TryParse(AuthenticateTimeoutString, out var authenticateTimeout))
                remoteConfig.AuthenticateTimeoutMinutes = authenticateTimeout;

        if (parts.TryGetValue("Server", out var serverUrl))
            remoteConfig.ServerUrl = serverUrl;

        if (parts.TryGetValue("Username", out var username) && parts.TryGetValue("Password", out var password))
        {
            remoteConfig.Credential = new Credential
            {
                UserName = username,
                Password = password
            };
        }

        return new StorageServerService(Options.Create(remoteConfig), new HttpClient(), dateTime);
    }

    // Delegate alle calls naar de gekozen implementation
    public Task<string?> GetStorageFileUrlAsync(string id, string type, CancellationToken ct) =>
        Implementation.GetStorageFileUrlAsync(id, type, ct);
    public Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct) =>
        Implementation.GetStorageFileUrlAsync(storageFile, ct);
    public Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true) =>
        Implementation.SaveStorageFileAsync(storageFile, fileName, mimeType, stream, ct, allowOverwrite);
    public Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false) =>
        Implementation.DeleteStorageFileAsync(storageFile, ct, throwIfNotFound);
}