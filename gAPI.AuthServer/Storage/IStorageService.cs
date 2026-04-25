using System.IO;
using System.Threading.Tasks;

namespace gAPI.Storage;


public interface IStorageService
{
    Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false);
    Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct);
    Task<string?> GetStorageFileUrlAsync(string id, string type, CancellationToken ct);
    Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true);
}