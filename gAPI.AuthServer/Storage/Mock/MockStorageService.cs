using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

#nullable enable
namespace gAPI.Storage.Mock;

public class MockStorageService : IStorageService
{
    private readonly MockStorageConfig Config;
    private static readonly ConcurrentDictionary<string, MockFileData> MockStorage = new();

    public MockStorageService(IOptions<MockStorageConfig> config)
    {
        Config = config.Value;
    }

    private string GetFileKey(IStorageFile storageFile)
    {
        return $"{storageFile.GetType().Name}/{storageFile.Id}";
    }
    private string GetFileKey(string type, string id)
    {
        return $"{type}/{id}";
    }
    private string GenerateMockUrl(string fileKey)
    {
        // Genereer een mock URL die er realistisch uitziet
        var baseUrl = Config.BaseUrl ?? "https://mock-storage.local";
        var hash = FileKeyHashHelper.GetFileKeyHash(fileKey);
        return $"{baseUrl}/files/{hash}/{Uri.EscapeDataString(fileKey)}";
    }

    public async Task<string?> GetStorageFileUrlAsync(string id, string type, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id) || id == "0")
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        await Task.Delay(Config.SimulateLatencyMs); // Simuleer netwerk latency

        var fileKey = GetFileKey(type, id);

        if (!MockStorage.ContainsKey(fileKey))
            return null;

        return GenerateMockUrl(fileKey);
    }
    public async Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        await Task.Delay(Config.SimulateLatencyMs); // Simuleer netwerk latency

        var fileKey = GetFileKey(storageFile);

        if (!MockStorage.ContainsKey(fileKey))
            return null;

        return GenerateMockUrl(fileKey);
    }
    public async Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                "Cannot use storage file server for entities without StorageFileName filled.");

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException(
                "Cannot use storage file server for entities without StorageMimeType filled.");

        await Task.Delay(Config.SimulateLatencyMs); // Simuleer upload tijd

        var fileKey = GetFileKey(storageFile);

        // Check overwrite
        if (!allowOverwrite && MockStorage.ContainsKey(fileKey))
            throw new Exception($"File already exists and overwrite is not allowed: {fileKey}");

        var mockFileData = await MockFileDataHelper.ProcessStreamAsync(stream, fileName, mimeType);
        MockStorage[fileKey] = mockFileData;

        return GenerateMockUrl(fileKey);
    }
    public async Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        await Task.Delay(Config.SimulateLatencyMs); // Simuleer netwerk latency

        var fileKey = GetFileKey(storageFile);
        var deleted = MockStorage.TryRemove(fileKey, out _);

        if (!deleted && throwIfNotFound)
            throw new Exception($"File not found: {fileKey}");

        return deleted;
    }

}