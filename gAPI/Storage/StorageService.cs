using gAPI.Storage.AzureStorage;
using gAPI.Storage.Mock;
using gAPI.Storage.StorageServer;
using gAPI.Storage.StorageServer.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace gAPI.Storage
{
    public class StorageService : IStorageService
    {
        private readonly IStorageService Implementation;

        public StorageService(IConfiguration configuration) :
            this(configuration.GetConnectionString("StorageConnection"))
        {
        }
        public StorageService(string? connectionString)
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
                "storageserver" => CreateStorageServerService(parts),
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


        private IStorageService CreateStorageServerService(Dictionary<string, string> parts)
        {
            var remoteConfig = new StorageServerConfig();

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

            return new StorageServerService(Options.Create(remoteConfig));
        }

        // Delegate alle calls naar de gekozen implementation
        public Task<string?> GetStorageFileUrlAsync(string id, string type) =>
            Implementation.GetStorageFileUrlAsync(id, type);

        public Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile) =>
            Implementation.GetStorageFileUrlAsync(storageFile);

        public Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, bool allowOverwrite = true) =>
            Implementation.SaveStorageFileAsync(storageFile, fileName, mimeType, stream, allowOverwrite);

        public Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, bool throwIfNotFound) =>
            Implementation.DeleteStorageFileAsync(storageFile, throwIfNotFound);

    }
}