using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using gAPI.Storage.Client.StorageServer;
using gAPI.Storage.Server.Services;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

#nullable enable
namespace gAPI.Core.Server.Storage.StorageServer;

public record StorageFileCacheValue(string? Url, DateTimeOffset Created);

public class StorageServerService(
    StorageServerConfig config,
    IGapiStorageService gapiStorageService,
    TimeProvider dateTime)
    : IStorageService
{
    private readonly ConcurrentDictionary<string, StorageFileCacheValue> UrlCache = new();

    private static string GetFileKey(IStorageFile storageFile)
    {
        return $"{storageFile.GetType().Name}/{storageFile.Id}";
    }

    public async Task<GetStorageFileInfoResponse> GetStorageFileInfo(IStorageFile storageFile, CancellationToken ct)
    {
        var key = GetFileKey(storageFile);
        return await GetStorageFileInfo(key, ct);
    }
    public async Task<GetStorageFileInfoResponse> GetStorageFileInfo(string key, CancellationToken ct)
    {
        var request = new GetStorageFileInfoRequest()
        {
            StorageKey = key
        };
        return await gapiStorageService.GetStorageFileUrl(request, ct);
    }

    public async Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct)
    {
        var key = GetFileKey(storageFile);
        return await GetStorageFileUrlAsync(key, ct);
    }
    public async Task<string?> GetStorageFileUrlAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");

        return UrlCache.AddOrUpdate(key,
            key =>
            {
                return UpdateUrlCache(key, ct);
            },
            (key, value) =>
            {
                if (value.Created > dateTime.GetUtcNow().AddSeconds(-config.UrlTimeoutSeconds)) return value;
                return UpdateUrlCache(key, ct);
            }).Url;
    }

    public async Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                $"Cannot use storage file server for entities without StorageFileName filled.");
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException(
                $"Cannot use storage file server for entities without StorageMimeType filled.");

        var key = GetFileKey(storageFile);
        return await SaveStorageFileAsync(key, fileName, mimeType, stream, ct, allowOverwrite);
    }
    public async Task<string?> SaveStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        var content = new MultipartFormDataContent();
        var saveRequest = new SaveRequest
        {
            StorageKey = key,
            FileName = fileName,
            MimeType = mimeType,
            AllowOverwrite = allowOverwrite
        };

        var enumerableStream = ReadStreamAsAsyncEnumerable(stream, config.MaxPackageSize ?? 64 * 1024, ct);
        var model = await gapiStorageService.SaveStorageFile(saveRequest, enumerableStream, ct);

        UrlCache[key] = new StorageFileCacheValue(model.Url, dateTime.GetUtcNow());
        return model.Url;
    }

    private async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadStreamAsAsyncEnumerable(Stream stream, int maxBuffer, [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = new byte[maxBuffer];
        var read = 0;
        while ((read = await stream.ReadAsync(buffer, 0, maxBuffer)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            yield return new ReadOnlyMemory<byte>(buffer, 0, read);
        }
        stream.Dispose();
    }

    public async Task<string?> AppendStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                $"Cannot use storage file server for entities without StorageFileName filled.");
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException(
                $"Cannot use storage file server for entities without StorageMimeType filled.");

        var key = GetFileKey(storageFile);
        return await AppendStorageFileAsync(key, fileName, mimeType, stream, ct, allowOverwrite);
    }
    public async Task<string?> AppendStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowCreate = true)
    {
        var content = new MultipartFormDataContent();
        var saveRequest = new AppendRequest
        {
            StorageKey = key,
            FileName = fileName,
            MimeType = mimeType,
            AllowCreate = allowCreate
        };

        var enumerableStream = ReadStreamAsAsyncEnumerable(stream, config.MaxPackageSize ?? 64 * 1024, ct);
        var model = await gapiStorageService.AppendStorageFile(saveRequest, enumerableStream, ct);

        UrlCache[key] = new StorageFileCacheValue(model.Url, dateTime.GetUtcNow());
        return model.Url;
    }

    public async Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");

        var key = GetFileKey(storageFile);
        return await DeleteStorageFileAsync(key, ct, throwIfNotFound);
    }
    public async Task<bool> DeleteStorageFileAsync(string key, CancellationToken ct, bool throwIfNotFound = false)
    {
        var request = new DeleteRequest()
        {
            StorageKey = key
        };
        var response = await gapiStorageService.DeleteStorageFile(request, ct);
        UrlCache.Remove(key, out _);
        return response.Deleted;
    }

    private StorageFileCacheValue UpdateUrlCache(string key, CancellationToken ct)
    {
        var request = new GetStorageFileInfoRequest()
        {
            StorageKey = key
        };
        var model = gapiStorageService.GetStorageFileUrl(request, ct).GetAwaiter().GetResult();
        return new(string.IsNullOrWhiteSpace(model.Url) ? null : model.Url, dateTime.GetUtcNow());
    }
}