using gAPI.Storage.StorageServer.Dtos.Requests;
using gAPI.Storage.StorageServer.Dtos.Responses;
using gAPI.Storage.Server.Data;
using gAPI.Storage.Server.Data.Entities;
using System.Security.Cryptography;

namespace gAPI.Storage.Server.Services;

public class LocalStorageService(ApplicationDbContext db)
{
    const string BaseFolder = "Content";
    static readonly DirectoryInfo Directory = new DirectoryInfo("Files");

    public GetStorageFileInfoResponse GetStorageFileUrl(GetStorageFileInfoRequest model)
    {
        var storageFolder = db.StorageFolders
            .FirstOrDefault(a => a.Name == model.TypeName);
        if (storageFolder == null)
            return new GetStorageFileInfoResponse()
            {
                Message = "Folder doesn't exists"
            };

        var directoryFullName = Path.Combine(Directory.FullName, model.TypeName);
        if (!System.IO.Directory.Exists(directoryFullName))
            return new GetStorageFileInfoResponse()
            {
                Message = "Folder doesn't exists on disk"
            };

        var storageFile = db.StorageFiles
            .FirstOrDefault(a =>
                a.StorageFolderId == storageFolder.Id &&
                a.EntityId == model.Id);
        if (storageFile?.FileName == null)
            return new GetStorageFileInfoResponse()
            {
                Message = "File doesn't exists"
            };

        var fullName = Path.Combine(directoryFullName, storageFile.FileName);
        if (!File.Exists(fullName))
            return new GetStorageFileInfoResponse()
            {
                Message = "File doesn't exists on disk"
            };

        var token = new StorageFileToken()
        {
            StorageFileId = storageFile.Id
        };

        var removeList = db.StorageFileTokens
            .Where(a => a.DateTime < DateTime.Now.AddMinutes(-15))
            .ToArray();
        foreach (var remove in removeList)
            db.StorageFileTokens.Remove(remove);
        db.StorageFileTokens.Add(token);
        db.SaveChanges();

        return new GetStorageFileInfoResponse()
        {
            Success = true,
            BaseUrl = model.BaseUrl,
            BaseFolder = BaseFolder,
            Folder = storageFolder.Name,
            FileName = storageFile.FileName,
            MimeType = storageFile.MimeType,
            EntityFileName = storageFile.EntityFileName,
            Length = storageFile.Length,
            Sha256 = storageFile.Sha256,
            Token = token.Token,
        };
    }

    public SaveResponse SaveStorageFile(SaveRequest model, Stream inputStream)
    {
        if (!Directory.Exists) Directory.Create();

        var fileName = $"{model.Id}{model.FileName}";
        var directoryFullName = Path.Combine(Directory.FullName, model.TypeName);
        var directoryInfo = new DirectoryInfo(directoryFullName);
        if (!directoryInfo.Exists) directoryInfo.Create();
        var fullName = Path.Combine(directoryFullName, fileName);

        var length = 0L;
        var sha256 = string.Empty;

        using (inputStream)
        using (var outputStream = File.Open(fullName, FileMode.OpenOrCreate))
        using (var hasher = SHA256.Create())
        {
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                outputStream.Write(buffer, 0, bytesRead);
                length += bytesRead;
            }

            hasher.TransformFinalBlock(new byte[0], 0, 0);
            var hashBytes = hasher.Hash;
            sha256 = ToHexStringLower(hashBytes!);
        }

        var storageFolder = db.StorageFolders.FirstOrDefault(a => a.Name == model.TypeName);
        if (storageFolder == null)
        {
            storageFolder = new StorageFolder()
            {
                Name = model.TypeName,
            };
            db.StorageFolders.Add(storageFolder);
            db.SaveChanges();
        }

        var storageFile = db.StorageFiles
            .FirstOrDefault(a =>
                a.StorageFolderId == storageFolder.Id &&
                a.EntityFileName == model.FileName);
        if (storageFile != null)
        {
            db.StorageFiles.Remove(storageFile);
            db.SaveChanges();
        }
        storageFile = new StorageFile()
        {
            EntityId = model.Id,
            EntityFileName = model.FileName,
            StorageFolderId = storageFolder.Id,
            FileName = fileName,
            Length = length,
            MimeType = model.MimeType,
            Sha256 = sha256
        };
        db.StorageFiles.Add(storageFile);
        db.SaveChanges();

        var externalUrlRequest = new GetStorageFileInfoRequest()
        {
            Id = model.Id,
            TypeName = model.TypeName,
            BaseUrl = model.BaseUrl
        };
        var externalUrlResponse = GetStorageFileUrl(externalUrlRequest);

        return new SaveResponse()
        {
            Success = true,
            Url = externalUrlResponse.Url,
            //StorageFileId = storageFile.Id,
            //EntityId = storageFile.EntityId,
            EntityFileName = storageFile.EntityFileName,
            FileName = storageFile.FileName,
            Length = storageFile.Length,
            MimeType = storageFile.MimeType,
            Sha256 = storageFile.Sha256
        };
    }

    public DeleteResponse DeleteStorageFile(DeleteRequest model)
    {
        var storageFolder = db.StorageFolders
            .FirstOrDefault(a => a.Name == model.TypeName);
        if (storageFolder == null)
            return new DeleteResponse()
            {
                Message = "Folder doesn't exists"
            };

        var directoryFullName = Path.Combine(Directory.FullName, model.TypeName);
        if (!System.IO.Directory.Exists(directoryFullName))
            return new DeleteResponse()
            {
                Message = "Folder doesn't exists on disk"
            };

        var storageFile = db.StorageFiles
            .FirstOrDefault(a =>
                a.StorageFolderId == storageFolder.Id &&
                a.EntityId == model.Id);
        if (storageFile?.FileName == null)
            return new DeleteResponse()
            {
                Message = "File doesn't exists"
            };

        var fullName = Path.Combine(directoryFullName, storageFile.FileName);
        if (!File.Exists(fullName))
            return new DeleteResponse()
            {
                Message = "File doesn't exists on disk"
            };

        // Forceer delete, wacht tot alle lezers weg zijn
        if (!ForceDelete(fullName))
            return new DeleteResponse()
            {
                Message = "File couldn't be deleted because it is in use, and we have waited 30sec while trying to exclusively lock the file, but that also failed. I failed you, I hope you try again in a few minutes because this should work, but I am just not worthy for the battle of exclusivity, just like reallife lol."
            };

        db.StorageFiles.Remove(storageFile);
        db.SaveChanges();

        return new DeleteResponse()
        {
            Success = true,
            Deleted = true
        };
    }

    private static string ToHexStringLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        int index = 0;
        foreach (var b in bytes)
        {
            chars[index++] = GetHexValue(b / 16);
            chars[index++] = GetHexValue(b % 16);
        }
        return new string(chars);

    }
    private static char GetHexValue(int i)
    {
        return (char)(i < 10 ? i + '0' : i - 10 + 'a');
    }
    private static bool ForceDelete(string path, int retryDelayMs = 200, int maxRetries = 150)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // Lock verkregen → meteen sluiten
                }
                File.Delete(path);
                return true; // klaar
            }
            catch (IOException)
            {
                // Bestand is nog in gebruik → wachten
                Thread.Sleep(retryDelayMs);
            }
            catch (UnauthorizedAccessException)
            {
                // Kan ook gebeuren bij file locks → zelfde gedrag
                Thread.Sleep(retryDelayMs);
            }
        }
        return false;
    }

}