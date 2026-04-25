using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#nullable enable
namespace gAPI.Storage.AzureStorage;

// This was fully written by Claude Sonnet 4, sorry I couldn't be bothered, not going to use it anyways
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

    private string GetBlobName(IStorageFile storageFile)
    {
        // Gebruik TypeName en Id voor unieke blob naam
        return $"{storageFile.GetType().Name}/{storageFile.Id}";
    }
    private string GetBlobName(string type, string id)
    {
        // Gebruik TypeName en Id voor unieke blob naam
        return $"{type}/{id}";
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken ct)
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient(Config.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, null, null, ct);
        return containerClient;
    }

    public async Task<string?> GetStorageFileUrlAsync(string id, string type, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id) || id == "0")
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        var containerClient = await GetContainerClientAsync(ct);
        var blobName = GetBlobName(type, id);
        return await GetStorageFileUrlAsync(containerClient, blobName, ct);
    }
    public async Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        var containerClient = await GetContainerClientAsync(ct);
        var blobName = GetBlobName(storageFile);
        return await GetStorageFileUrlAsync(containerClient, blobName, ct);
    }
    public async Task<string?> GetStorageFileUrlAsync(BlobContainerClient containerClient, string blobName, CancellationToken ct)
    {
        var blobClient = containerClient.GetBlobClient(blobName);

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
                BlobName = blobName,
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

        var containerClient = await GetContainerClientAsync(ct);
        var blobName = GetBlobName(storageFile);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Check of bestand al bestaat als overwrite niet is toegestaan
        if (!allowOverwrite)
        {
            var exists = await blobClient.ExistsAsync(ct);
            if (exists.Value)
                throw new Exception($"File already exists and overwrite is not allowed: {blobName}");
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
                ["TypeName"] = storageFile.GetType().Name,
                ["UploadedAt"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };

        var response = await blobClient.UploadAsync(stream, uploadOptions, ct);

        // Genereer URL voor direct gebruik
        return await GetStorageFileUrlAsync(storageFile, ct);
    }
    public async Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                "Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext yet.");

        var containerClient = await GetContainerClientAsync(ct);
        var blobName = GetBlobName(storageFile);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, ct);

        if (!response.Value && throwIfNotFound)
            throw new Exception($"File not found: {blobName}");

        return response.Value;
    }

}