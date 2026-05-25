using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using gAPI.Core.Server.Storage.StorageServer.Enums;
using gAPI.Storage.Server.Data;
using gAPI.Storage.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace gAPI.Storage.Server.Services;

public class LocalStorageService(ApplicationDbContext db)
{
    const string BaseFolder = "Content";
    static readonly DirectoryInfo Directory = new("Files");

    public async Task<GetStorageFileInfoResponse> GetStorageFileUrl(GetStorageFileInfoRequest model, CancellationToken ct)
    {
        (string directoryName, string fileName) = GetDirectoryAndFileName(model.StorageKey);

        var storageFolder = db.StorageFolders
            .FirstOrDefaultAsync(a => a.Name == directoryName, ct);
        if (storageFolder == null)
            return new GetStorageFileInfoResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FolderDoesntExists
            };

        var directoryFullName = Path.Combine(Directory.FullName, directoryName);
        if (!System.IO.Directory.Exists(directoryFullName))
            return new GetStorageFileInfoResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FolderDoesntExistsOnDisk
            };

        var storageFile = await db.StorageFiles
            .FirstOrDefaultAsync(a =>
                a.StorageFolderId == storageFolder.Id &&
                a.Key == model.StorageKey, ct);
        if (storageFile?.FileName == null)
            return new GetStorageFileInfoResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FileDoesntExists
            };

        var fullName = Path.Combine(directoryFullName, storageFile.FileName);
        if (!File.Exists(fullName))
            return new GetStorageFileInfoResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FileDoesntExistsOnDisk
            };

        var token = new StorageFileToken()
        {
            StorageFileId = storageFile.Id
        };

        var now = DateTime.Now;
        var removeList = await db.StorageFileTokens
            .Where(a => a.DateTime < now.AddMinutes(-15))
            .ToArrayAsync();
        foreach (var remove in removeList)
            db.StorageFileTokens.Remove(remove);
        await db.StorageFileTokens.AddAsync(token, ct);
        await db.SaveChangesAsync(ct);

        return new GetStorageFileInfoResponse()
        {
            Success = true,
            BaseUrl = model.BaseUrl,
            BaseFolder = BaseFolder,
            FileInfo = new()
            {
                Key = storageFile.Key,
                MimeType = storageFile.MimeType,
                FileName = storageFile.FileName,
                Length = storageFile.Length
            },
            Token = token.Token,
        };
    }

    public async Task<SaveResponse> SaveStorageFile(SaveRequest model, Stream inputStream, CancellationToken ct)
    {
        (string directoryName, string fileName) = GetDirectoryAndFileName(model.StorageKey);

        if (!Directory.Exists) Directory.Create();

        var directoryFullName = Path.Combine(Directory.FullName, directoryName);
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
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                await outputStream.WriteAsync(buffer, 0, bytesRead, ct);
                length += bytesRead;
            }

            hasher.TransformFinalBlock([], 0, 0);
            var hashBytes = hasher.Hash;
            sha256 = ToHexStringLower(hashBytes!);
        }

        var storageFolder = await db.StorageFolders.FirstOrDefaultAsync(a => a.Name == directoryName, ct);
        if (storageFolder == null)
        {
            storageFolder = new StorageFolder()
            {
                Name = directoryName,
            };
            db.StorageFolders.Add(storageFolder);
            db.SaveChanges();
        }

        var storageFile = await db.StorageFiles
            .FirstOrDefaultAsync(a =>
                a.StorageFolderId == storageFolder.Id &&
                a.FileName == model.FileName, ct);
        if (storageFile != null)
        {
            db.StorageFiles.Remove(storageFile);
            await db.SaveChangesAsync(ct);
        }
        storageFile = new StorageFile()
        {
            Key = model.StorageKey,
            StorageFolderId = storageFolder.Id,
            FileName = fileName,
            Length = length,
            MimeType = model.MimeType
        };
        await db.StorageFiles.AddAsync(storageFile, ct);
        await db.SaveChangesAsync(ct);

        var externalUrlRequest = new GetStorageFileInfoRequest()
        {
            StorageKey = model.StorageKey,
            BaseUrl = model.BaseUrl
        };
        var externalUrlResponse = await GetStorageFileUrl(externalUrlRequest, ct);

        return new SaveResponse()
        {
            Success = true,
            Url = externalUrlResponse.Url,
            FileInfo = new()
            {
                Key = storageFile.Key,
                MimeType = storageFile.MimeType,
                FileName = storageFile.FileName,
                Length = storageFile.Length
            },
        };
    }
    public async Task<AppendResponse> AppendStorageFile(AppendRequest model, Stream inputStream, CancellationToken ct)
    {
        var split = model.StorageKey.Split('/');
        var typeName = "_";
        var id = model.StorageKey;
        if (split.Length > 1)
        {
            typeName = split[0];
            id = id.Substring(typeName.Length + 1);
        }

        if (!Directory.Exists) Directory.Create();

        var fileName = $"{model.StorageKey}{model.FileName}";
        var directoryFullName = Path.Combine(Directory.FullName, typeName);
        var directoryInfo = new DirectoryInfo(directoryFullName);
        if (!directoryInfo.Exists) directoryInfo.Create();
        var fullName = Path.Combine(directoryFullName, fileName);

        var length = 0L;

        using (inputStream)
        using (var outputStream = File.Open(fullName, FileMode.Append))
        {
            length = outputStream.Position;

            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead, ct);
                length += bytesRead;
            }
        }

        var storageFolder = await db.StorageFolders.FirstOrDefaultAsync(a => a.Name == typeName, ct);
        if (storageFolder == null)
        {
            storageFolder = new StorageFolder()
            {
                Name = typeName,
            };
            await db.StorageFolders.AddAsync(storageFolder, ct);
            await db.SaveChangesAsync(ct);
        }

        var storageFile = await db.StorageFiles
            .FirstOrDefaultAsync(a =>
                a.StorageFolderId == storageFolder.Id &&
                a.Key == model.StorageKey, ct);
        if (storageFile != null)
        {
            db.StorageFiles.Remove(storageFile);
           await db.SaveChangesAsync(ct);
        }
        storageFile = new StorageFile()
        {
            Key = model.StorageKey,
            StorageFolderId = storageFolder.Id,
            FileName = fileName,
            Length = length,
            MimeType = model.MimeType
        };
        await db.StorageFiles.AddAsync(storageFile, ct);
        await db.SaveChangesAsync(ct);

        var externalUrlRequest = new GetStorageFileInfoRequest()
        {
            StorageKey = model.StorageKey,
            BaseUrl = model.BaseUrl
        };
        var externalUrlResponse = await GetStorageFileUrl(externalUrlRequest, ct);

        return new AppendResponse()
        {
            Success = true,
            Url = externalUrlResponse.Url,
            FileInfo = new()
            {
                Key = storageFile.Key,
                MimeType = storageFile.MimeType,
                FileName = storageFile.FileName,
                Length = storageFile.Length
            },
        };
    }

    public async Task<DeleteResponse> DeleteStorageFile(DeleteRequest model, CancellationToken ct)
    {
        var split = model.StorageKey.Split('/');
        var typeName = "_";
        var id = model.StorageKey;
        if (split.Length > 1)
        {
            typeName = split[0];
            id = id.Substring(typeName.Length + 1);
        }

        var storageFolder = await db.StorageFolders
            .FirstOrDefaultAsync(a => a.Name == typeName, ct);
        if (storageFolder == null)
            return new DeleteResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FolderDoesntExists
            };

        var directoryFullName = Path.Combine(Directory.FullName, typeName);
        if (!System.IO.Directory.Exists(directoryFullName))
            return new DeleteResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FolderDoesntExistsOnDisk
            };

        var storageFile = await db.StorageFiles
            .FirstOrDefaultAsync(a =>
                a.StorageFolderId == storageFolder.Id &&
                a.Key == model.StorageKey, ct);
        if (storageFile?.FileName == null)
            return new DeleteResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FileDoesntExists
            };

        var fullName = Path.Combine(directoryFullName, storageFile.FileName);
        if (!File.Exists(fullName))
            return new DeleteResponse()
            {
                ErrorMessage = ErrorMessagesEnum.FileDoesntExistsOnDisk
            };

        // Forceer delete, wacht tot alle lezers weg zijn
        if (!ForceDelete(fullName))
            return new DeleteResponse()
            {
                ErrorMessage = ErrorMessagesEnum.CouldNotDeleteFileInUse
            };

        db.StorageFiles.Remove(storageFile);
        db.SaveChanges();

        return new DeleteResponse()
        {
            Success = true,
            Deleted = true
        };
    }




    private static (string directoryName, string fileName) GetDirectoryAndFileName(string storageKey)
    {
        var split = storageKey.Split('/');
        var directoryName = "_";
        var fileName = storageKey;
        if (split.Length > 1)
        {
            directoryName = split[0];
            fileName = storageKey.Substring(directoryName.Length + 1);
        }
        return new(directoryName, fileName);
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