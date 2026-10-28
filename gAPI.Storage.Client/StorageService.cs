using gAPI.Core.Server.Storage.AzureStorage;
using gAPI.Core.Server.Storage.Mock;
using gAPI.Core.Server.Storage.StorageServer;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using gAPI.Storage.Client;
using gAPI.Storage.Client.StorageServer;
using gAPI.Storage.Server.Services;
using Microsoft.Extensions.Options;

namespace gAPI.Core.Server.Storage;

public class StorageService : IStorageService
{
    private readonly IStorageService Implementation;

    public StorageConfig Config { get; }
    public IGapiStorageService? GapiStorageServices { get; }

    public StorageService(
        IEnumerable<IGapiStorageService> gapiStorageServices,
        StorageConfig config,
        TimeProvider dateTime)
    {
        Config = config;
        GapiStorageServices = gapiStorageServices.LastOrDefault();

        if (string.IsNullOrWhiteSpace(config.StorageConnectionString))
            throw new Exception("StorageConnection ConnectionString is required");

        // Parse connection string
        var parts = config.StorageConnectionString!.Split(';')
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

    private static MockStorageService CreateMockService(Dictionary<string, string> parts)
    {
        var mockConfig = new MockStorageConfig();

        if (parts.TryGetValue("BaseUrl", out var baseUrl))
            mockConfig.BaseUrl = baseUrl;

        if (parts.TryGetValue("LatencyMs", out var latencyStr) && int.TryParse(latencyStr, out var latency))
            mockConfig.SimulateLatencyMs = latency;

        return new MockStorageService(Options.Create(mockConfig));
    }
    private static AzureStorageService CreateAzureService(Dictionary<string, string> parts)
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
    private StorageServerService CreateStorageServerService(Dictionary<string, string> parts, TimeProvider dateTime)
    {
        if (GapiStorageServices == null)
            throw new Exception("AutoBus is not installed");

        if (parts.TryGetValue("UrlTimeout", out var urlTimeoutString) == false)
            throw new Exception("Cannot find UrlTimeout");
        if (int.TryParse(urlTimeoutString, out var urlTimeout) == false)
            throw new Exception("Cannot find UrlTimeout");

        if (parts.TryGetValue("AuthenticateTimeout", out var AuthenticateTimeoutString) == false)
            throw new Exception("Cannot find authenticateTimeout");
        if (int.TryParse(AuthenticateTimeoutString, out var authenticateTimeout) == false)
            throw new Exception("Cannot find authenticateTimeout");

        var remoteConfig = new StorageServerConfig(urlTimeout, authenticateTimeout, Config.MaxPackageSize);
        return new StorageServerService(remoteConfig, GapiStorageServices, dateTime);
    }

    // Delegate alle calls naar de gekozen implementation
    public Task<GetStorageFileInfoResponse> GetStorageFileInfo(IStorageFile storageFile, CancellationToken ct) =>
        Implementation.GetStorageFileInfo(storageFile, ct);
    public Task<GetStorageFileInfoResponse> GetStorageFileInfo(string key, CancellationToken ct) =>
        Implementation.GetStorageFileInfo(key, ct);

    public Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct) =>
        Implementation.GetStorageFileUrlAsync(storageFile, ct);
    public Task<string?> GetStorageFileUrlAsync(string key, CancellationToken ct) =>
        Implementation.GetStorageFileUrlAsync(key, ct);

    public Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true) =>
        Implementation.SaveStorageFileAsync(storageFile, fileName, mimeType, stream, ct, allowOverwrite);
    public Task<string?> SaveStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true) =>
        Implementation.SaveStorageFileAsync(key, fileName, mimeType, stream, ct, allowOverwrite);

    public Task<string?> AppendStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowCreate = true) =>
        Implementation.AppendStorageFileAsync(storageFile, fileName, mimeType, stream, ct, allowCreate);
    public Task<string?> AppendStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowCreate = true) =>
        Implementation.AppendStorageFileAsync(key, fileName, mimeType, stream, ct, allowCreate);

    public Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false) =>
        Implementation.DeleteStorageFileAsync(storageFile, ct, throwIfNotFound);
    public Task<bool> DeleteStorageFileAsync(string key, CancellationToken ct, bool throwIfNotFound = false) =>
        Implementation.DeleteStorageFileAsync(key, ct, throwIfNotFound);
}