using gAPI.Storage.Core;
using gAPI.Storage.Core.Dtos.Responses;

namespace gAPI.Storage.Client;


public interface IStorageService
{
    Task<GetStorageFileInfoResponse> GetStorageFileInfo(IStorageFile storageFile, CancellationToken ct);
    Task<GetStorageFileInfoResponse> GetStorageFileInfo(string key, CancellationToken ct);

    Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct);
    Task<string?> GetStorageFileUrlAsync(string key, CancellationToken ct);

    Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true);
    Task<string?> SaveStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true);

    Task<string?> AppendStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowCreate = true);
    Task<string?> AppendStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowCreate = true);

    Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false);
    Task<bool> DeleteStorageFileAsync(string key, CancellationToken ct, bool throwIfNotFound = false);
}