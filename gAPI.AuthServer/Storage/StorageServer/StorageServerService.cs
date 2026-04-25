using gAPI.Storage.StorageServer.Dtos.Requests;
using gAPI.Storage.StorageServer.Dtos.Responses;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;

#nullable enable
namespace gAPI.Storage.StorageServer;

public class StorageServerService : IStorageService
{
    private record StorageFileCacheKey(string id, string type);
    private record StorageFileCacheValue(string? url, DateTimeOffset created);

    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly string ServerUrl;
    private readonly ConcurrentDictionary<StorageFileCacheKey, StorageFileCacheValue> UrlCache = new();
    private readonly IOptions<StorageServerConfig> Config;
    private readonly HttpClient HttpClient;
    private readonly TimeProvider DateTime;
    private DateTimeOffset? LastAuthenticate;

    public StorageServerService(
        IOptions<StorageServerConfig> config,
        HttpClient httpClient,
        TimeProvider dateTime)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.Value.ServerUrl) || config.Value.ServerUrl == null)
            throw new Exception("StorageServerConfig.ServerUrl is not set. Please provide a valid server URL.");

        ServerUrl = config.Value.ServerUrl;

        Config = config;
        HttpClient = httpClient;
        DateTime = dateTime;
        HttpClient.BaseAddress = new Uri(ServerUrl);
    }

    public async Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile, CancellationToken ct)
    {
        return await GetStorageFileUrlAsync(storageFile.Id, storageFile.GetType().Name, ct);
    }
    public async Task<string?> GetStorageFileUrlAsync(string id, string type, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id) || id == "0" || id == null)
            throw new ArgumentException(
                $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");

        var key = new StorageFileCacheKey(id, type);
        return UrlCache.AddOrUpdate(key, 
            (StorageFileCacheKey key) =>
            {
                AuthenticateHttpClient(ct).GetAwaiter().GetResult();
                return Update(key);
            },
            (StorageFileCacheKey key, StorageFileCacheValue value) =>
            {
                if (value.created > DateTime.GetUtcNow().AddSeconds(-Config.Value.UrlTimeoutSeconds)) return value;
                return Update(key);
            }).url;
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

        await AuthenticateHttpClient(ct);

        var storageFileTypeName = storageFile.GetType().Name;
        var storageFileId = storageFile.Id;
        return await SaveStorageFileAsync(storageFileTypeName, storageFileId, fileName, mimeType, stream, ct, allowOverwrite);
    }
    public async Task<string?> SaveStorageFileAsync(string storageFileTypeName, string storageFileId, string fileName, string mimeType, Stream stream, CancellationToken ct, bool allowOverwrite = true)
    {
        var content = new MultipartFormDataContent();
        var saveRequest = new SaveRequest
        {
            TypeName = storageFileTypeName,
            Id = storageFileId,
            FileName = fileName,
            MimeType = mimeType,
            AllowOverwrite = allowOverwrite,
            BaseUrl = ServerUrl
        };

        // Voeg JSON-velden als string toe
        content.Add(new StringContent(saveRequest.Id.ToString()), nameof(SaveRequest.Id));
        content.Add(new StringContent(saveRequest.FileName), nameof(SaveRequest.FileName));
        content.Add(new StringContent(saveRequest.TypeName), nameof(SaveRequest.TypeName));
        content.Add(new StringContent(saveRequest.MimeType), nameof(SaveRequest.MimeType));
        content.Add(new StringContent(saveRequest.BaseUrl), nameof(SaveRequest.BaseUrl));
        content.Add(new StringContent(saveRequest.AllowOverwrite ? "true" : "false"), nameof(SaveRequest.AllowOverwrite));

        // Voeg bestand toe
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "file", fileName);

        using var response = await HttpClient.PostAsync("/Storage/SaveStorageFile", content);
        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<SaveResponse>();
        if (model == null)
            throw new Exception("Could not cast response from file server");
        if (!model.Success)
            throw new Exception(model.Message);
        if (model.Url == null)
            throw new Exception("Url is empty");
        if (!Uri.IsWellFormedUriString(model.Url, UriKind.Absolute))
            throw new Exception("getExternalUrlResponse.Url is not set or is not a valid URL. Please provide a valid server URL.");

        var key = new StorageFileCacheKey(saveRequest.Id, saveRequest.TypeName);
        UrlCache[key] = new StorageFileCacheValue(model.Url, DateTime.GetUtcNow());
        return model.Url;
    }

    public async Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");

        await AuthenticateHttpClient(ct);

        var storageFileId = storageFile.Id;
        var storageFileTypeName = storageFile.GetType().Name;
        return await DeleteStorageFileAsync(storageFileTypeName, storageFileId, ct, throwIfNotFound);
    }
    public async Task<bool> DeleteStorageFileAsync(string storageFileTypeName, string storageFileId, CancellationToken ct, bool throwIfNotFound = false)
    {
        var request = new DeleteRequest()
        {
            TypeName = storageFileTypeName,
            Id = storageFileId,
            BaseUrl = ServerUrl
        };

        using var responseMessage = await HttpClient.PostAsJsonAsync("/Storage/DeleteStorageFile", request);
        responseMessage.EnsureSuccessStatusCode();

        var response = await responseMessage.Content.ReadFromJsonAsync<DeleteResponse>();
        if (response == null)
            throw new Exception("Could not cast response from file server");
        if (throwIfNotFound && response.Success == false)
            throw new Exception(response.Message);

        var key = new StorageFileCacheKey(request.Id, request.TypeName);
        UrlCache.Remove(key, out _);
        return response.Deleted;
    }

    private async Task AuthenticateHttpClient(CancellationToken ct, bool forceReload = false)
    {
        // Fast-path (geen lock, alleen lezen)
        if (!forceReload &&
            LastAuthenticate is DateTimeOffset last &&
            last > DateTime.GetUtcNow().AddMinutes(-Config.Value.AuthenticateTimeoutMinutes))
        {
            return;
        }

        await _authLock.WaitAsync();
        try
        {
            // Double-check na lock (BELANGRIJK)
            if (!forceReload &&
                LastAuthenticate is DateTimeOffset lockedLast &&
                lockedLast > DateTime.GetUtcNow().AddMinutes(-Config.Value.AuthenticateTimeoutMinutes))
            {
                return;
            }

            // 👉 hier je echte authenticate code
            await DoAuthenticateAsync(ct);

            // Pas NA succesvolle authenticate
            LastAuthenticate = DateTime.GetUtcNow();
        }
        finally
        {
            _authLock.Release();
        }
    }
    private async Task DoAuthenticateAsync(CancellationToken ct)
    {
        var credential = Config.Value.Credential;
        if (credential == null || string.IsNullOrWhiteSpace(credential.UserName) || string.IsNullOrWhiteSpace(credential.Password))
            throw new Exception("StorageServerConfig.Credential is not set or incomplete. Please provide valid credentials.");

        if (Config.Value.ServerUrl == null || !Uri.IsWellFormedUriString(Config.Value.ServerUrl, UriKind.Absolute))
            throw new Exception("StorageServerConfig.ServerUrl is not set or is not a valid URL. Please provide a valid server URL.");

        if (string.IsNullOrWhiteSpace(credential.UserName) || credential.UserName == null)
            throw new Exception("credential.UserName is requered");
        if (string.IsNullOrWhiteSpace(credential.Password) || credential.Password == null)
            throw new Exception("credential.UserName is requered");


        // Stap 1: Login
        var loginRequest = new LoginRequest
        {
            Username = credential.UserName,
            Password = credential.Password
        };

        //Console.WriteLine($"StorageServerService.AuthenticateHttpClient UserName={credential.UserName}");

        using var response = await HttpClient.PostAsJsonAsync("/Auth/Login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        var jwtToken = loginResult?.Token;
        if (jwtToken == null) throw new Exception("Kan geen token ophalen");

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
    }

    //private StorageFileCacheValue Add(StorageFileCacheKey key)
    //{
    //    AuthenticateHttpClient(ct).GetAwaiter().GetResult();
    //    return Update(key);
    //}
    //private StorageFileCacheValue TryUpdate(StorageFileCacheKey key, StorageFileCacheValue value)
    //{
    //    if (value.created > DateTime.GetUtcNow().AddSeconds(-Config.Value.UrlTimeoutSeconds)) return value;
    //    return Update(key);
    //}
    private StorageFileCacheValue Update(StorageFileCacheKey key)
    {
        var request = new GetStorageFileInfoRequest()
        {
            Id = key.id,
            TypeName = key.type,
            BaseUrl = ServerUrl
        };

        using var response = HttpClient.PostAsJsonAsync("/Storage/GetStorageFileInfo", request).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var model = response.Content.ReadFromJsonAsync<GetStorageFileInfoResponse>().GetAwaiter().GetResult();
        if (model == null)
            throw new Exception("Could not cast response from file server");
        if (!string.IsNullOrWhiteSpace(model.Url) && !Uri.IsWellFormedUriString(model.Url, UriKind.Absolute))
            throw new Exception("getExternalUrlResponse.Url is not set or is not a valid URL. Please provide a valid server URL.");

        return new(string.IsNullOrWhiteSpace(model.Url) ? null : model.Url, DateTime.GetUtcNow());
    }
}