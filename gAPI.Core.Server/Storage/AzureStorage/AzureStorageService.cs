using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using Microsoft.Extensions.Options;

#nullable enable
namespace gAPI.Core.Server.Storage.AzureStorage;

public class AzureStorageService : IStorageService
{
    private readonly BlobServiceClient BlobServiceClient;
    private readonly AzureStorageConfig Config;

    public AzureStorageService(IOptions<AzureStorageConfig> config)
    {
        Config = config.Value;

        if (string.IsNullOrWhiteSpace(Config.ConnectionString))
            throw new Exception("AzureStorageConfig.ConnectionString is not set. Please provide a valid connection string.");

        if (string.IsNullOrWhiteSpace(Config.ContainerName))
            throw new Exception("AzureStorageConfig.ContainerName is not set. Please provide a valid container name.");

        BlobServiceClient = new BlobServiceClient(Config.ConnectionString);
    }

    private static string GetBlobName(IStorageFile storageFile)
    {
        // Gebruik TypeName en Id voor unieke blob naam
        return $"{storageFile.GetType().Name}/{storageFile.Id}";
    }
    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken ct)
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient(Config.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, null, null, ct);
        return containerClient;
    }

    public async Task<GetStorageFileInfoResponse> GetStorageFileInfo(string key, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new GetStorageFileInfoResponse
                {
                    Success = false,
                    ErrorMessage = "Key is empty."
                };
            }

            var containerClient = await GetContainerClientAsync(ct);

            var blobClient = containerClient.GetBlobClient(key);

            var exists = await blobClient.ExistsAsync(ct);

            if (!exists.Value)
            {
                return new GetStorageFileInfoResponse
                {
                    Success = false,
                    ErrorMessage = $"File not found: {key}"
                };
            }

            var properties = await blobClient.GetPropertiesAsync(null, ct);

            // Folder/FileName afsplitsen
            var lastSlash = key.LastIndexOf('/');

            string? folder = null;
            string? fileName = key;

            if (lastSlash >= 0)
            {
                folder = key[..lastSlash];
                fileName = key[(lastSlash + 1)..];
            }

            // SAS token genereren
            string? token = null;

            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = Config.ContainerName,
                    BlobName = key,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);

                token = sasUri.Query.TrimStart('?');
            }

            return new GetStorageFileInfoResponse
            {
                Success = true,

                BaseUrl = $"{blobClient.Uri.Scheme}://{blobClient.Uri.Host}" +
                          (blobClient.Uri.Port is > 0 and not 80
                              ? $":{blobClient.Uri.Port}"
                              : ""),

                // Azure blobs hebben niet echt folders
                BaseFolder = Config.ContainerName,

                Folder = folder,
                FileName = fileName,

                Token = token,

                MimeType = properties.Value.ContentType,
                Length = properties.Value.ContentLength,

                EntityFileName =
                    properties.Value.Metadata.TryGetValue("OriginalFileName", out var originalName)
                        ? originalName
                        : null
            };
        }
        catch (Exception ex)
        {
            return new GetStorageFileInfoResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct)
    {
        var key = GetBlobName(storageFile);
        return GetStorageFileUrlAsync(key, ct);
    }
    public async Task<string?> GetStorageFileUrlAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        var containerClient = await GetContainerClientAsync(ct);
        var blobClient = containerClient.GetBlobClient(key);

        // Check of blob bestaat
        var exists = await blobClient.ExistsAsync(ct);
        if (!exists.Value)
            return null;

        // Genereer SAS URL met 15 minuten expiry
        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = Config.ContainerName,
                BlobName = key,
                Resource = "b", // blob
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
        else
        {
            // Fallback als SAS niet mogelijk is
            return blobClient.Uri.ToString();
        }
    }

    public Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        var key = GetBlobName(storageFile);
        return SaveStorageFileAsync(key, fileName, mimeType, stream, ct, allowOverwrite);
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

        var containerClient = await GetContainerClientAsync(ct);
        var blobClient = containerClient.GetBlobClient(key);

        // Check of bestand al bestaat als overwrite niet is toegestaan
        if (!allowOverwrite)
        {
            var exists = await blobClient.ExistsAsync(ct);
            if (exists.Value)
                throw new Exception($"File already exists and overwrite is not allowed: {key}");
        }

        // Reset stream position
        if (stream.CanSeek)
            stream.Position = 0;

        // Upload blob
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = mimeType
            },
            Metadata = new Dictionary<string, string>
            {
                ["OriginalFileName"] = fileName,
                ["Key"] = key,
                ["UploadedAt"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };

        await blobClient.UploadAsync(stream, uploadOptions, ct);

        // Genereer URL voor direct gebruik
        return await GetStorageFileUrlAsync(key, ct);
    }

    public Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false)
    {
        var key = GetBlobName(storageFile);
        return DeleteStorageFileAsync(key, ct, throwIfNotFound);
    }
    public async Task<bool> DeleteStorageFileAsync(string key, CancellationToken ct, bool throwIfNotFound = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        var containerClient = await GetContainerClientAsync(ct);
        var blobClient = containerClient.GetBlobClient(key);

        var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, ct);

        if (!response.Value && throwIfNotFound)
            throw new Exception($"File not found: {key}");

        return response.Value;
    }

    public Task<string?> AppendStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        var key = GetBlobName(storageFile);

        return AppendStorageFileAsync(
            key,
            fileName,
            mimeType,
            stream,
            ct,
            allowOverwrite);
    }
    public async Task<string?> AppendStorageFileAsync(string key, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowCreate = true)
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

        var containerClient = await GetContainerClientAsync(ct);

        var appendBlobClient = containerClient.GetAppendBlobClient(key);

        // Maak blob aan indien nodig
        var exists = await appendBlobClient.ExistsAsync(ct);

        if (!exists.Value)
        {
            if (!allowCreate)
                throw new Exception($"Append blob does not exist: {key}");

            await appendBlobClient.CreateAsync(
                new AppendBlobCreateOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = mimeType
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["OriginalFileName"] = fileName,
                        ["Key"] = key,
                        ["UploadedAt"] = DateTimeOffset.UtcNow.ToString("O")
                    }
                },
                ct);
        }

        // Reset stream positie
        if (stream.CanSeek)
            stream.Position = 0;

        // Append data
        await appendBlobClient.AppendBlockAsync(stream, cancellationToken: ct);

        return await GetStorageFileUrlAsync(key, ct);
    }
}