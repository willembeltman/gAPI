using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

#nullable enable
namespace gAPI.Core.Server.Storage.Mock;

public class MockStorageService(IOptions<MockStorageConfig> config) : IStorageService
{
    private readonly MockStorageConfig Config = config.Value;
    private static readonly ConcurrentDictionary<string, MockFileData> MockStorage = new();

    private static string GetFileKey(IStorageFile storageFile)
    {
        return $"{storageFile.GetType().Name}/{storageFile.Id}";
    }
    private string GenerateMockUrl(string fileKey)
    {
        // Genereer een mock URL die er realistisch uitziet
        var baseUrl = Config.BaseUrl ?? "https://mock-storage.local";
        var hash = FileKeyHashHelper.GetFileKeyHash(fileKey);
        return $"{baseUrl}/files/{hash}/{Uri.EscapeDataString(fileKey)}";
    }

    public Task<GetStorageFileInfoResponse> GetStorageFileInfo(IStorageFile storageFile, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
    public Task<GetStorageFileInfoResponse> GetStorageFileInfo(string key, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct)
    {
        var key = GetFileKey(storageFile);
        return GetStorageFileUrlAsync(key, ct);
    }
    public async Task<string?> GetStorageFileUrlAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        await Task.Delay(Config.SimulateLatencyMs, ct); // Simuleer netwerk latency


        if (!MockStorage.ContainsKey(key))
            return null;

        return GenerateMockUrl(key);
    }

    public async Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        var key = GetFileKey(storageFile);
        return await SaveStorageFileAsync(key, fileName, mimeType, stream, ct, allowOverwrite);
    }
    public async Task<string?> SaveStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                "Cannot use storage file server for entities without StorageFileName filled.");

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException(
                "Cannot use storage file server for entities without StorageMimeType filled.");

        await Task.Delay(Config.SimulateLatencyMs); // Simuleer upload tijd

        // Check overwrite
        if (!allowOverwrite && MockStorage.ContainsKey(key))
            throw new Exception($"File already exists and overwrite is not allowed: {key}");

        var mockFileData = await MockFileDataHelper.ProcessStreamAsync(stream, fileName, mimeType);
        MockStorage[key] = mockFileData;

        return GenerateMockUrl(key);
    }

    public Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false)
    {
        var key = GetFileKey(storageFile);
        return DeleteStorageFileAsync(key, ct, throwIfNotFound);
    }
    public async Task<bool> DeleteStorageFileAsync(string key, CancellationToken ct, bool throwIfNotFound = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        await Task.Delay(Config.SimulateLatencyMs); // Simuleer netwerk latency

        var deleted = MockStorage.TryRemove(key, out _);

        if (!deleted && throwIfNotFound)
            throw new Exception($"File not found: {key}");

        return deleted;
    }

    public Task<string?> AppendStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        throw new NotImplementedException();
    }
    public Task<string?> AppendStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowCreate = true)
    {
        throw new NotImplementedException();
    }

}