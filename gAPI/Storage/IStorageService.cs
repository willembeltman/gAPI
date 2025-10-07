using System.IO;
using System.Threading.Tasks;

namespace gAPI.Storage
{

    public interface IStorageService
    {
        Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, bool throwIfNotFound);
        Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile);
        Task<string?> GetStorageFileUrlAsync(string id, string type);
        Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, bool allowOverwrite = true);
    }
}