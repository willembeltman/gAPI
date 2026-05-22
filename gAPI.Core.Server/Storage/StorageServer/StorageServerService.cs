using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;

#nullable enable
namespace gAPI.Core.Server.Storage.StorageServer;

//public record StorageFileCacheKey(string Id, string Type);
public record StorageFileCacheValue(string? Url, DateTimeOffset Created);

public class StorageServerService : IStorageService
{
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly string ServerUrl;
    private readonly ConcurrentDictionary<string, StorageFileCacheValue> UrlCache = new();
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


    private string GetFileKey(IStorageFile storageFile)
    {
        return $"{storageFile.GetType().Name}/{storageFile.Id}";
    }

    public async Task<GetStorageFileInfoResponse> GetStorageFileInfo(string key, CancellationToken ct)
    {
        var request = new GetStorageFileInfoRequest()
        {
            Key = key,
            BaseUrl = ServerUrl
        };

        using var response = await HttpClient.PostAsJsonAsync("/Storage/GetStorageFileInfo", request, ct);
        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<GetStorageFileInfoResponse>(ct)
            ?? throw new Exception("Could not cast response from file server");
        if (!string.IsNullOrWhiteSpace(model.Url) && !Uri.IsWellFormedUriString(model.Url, UriKind.Absolute))
            throw new Exception("getExternalUrlResponse.Url is not set or is not a valid URL. Please provide a valid server URL.");

        return model;
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
                AuthenticateHttpClient(ct).GetAwaiter().GetResult();
                return UpdateUrlCache(key);
            },
            (key, value) =>
            {
                if (value.Created > DateTime.GetUtcNow().AddSeconds(-Config.Value.UrlTimeoutSeconds)) return value;
                return UpdateUrlCache(key);
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
        await AuthenticateHttpClient(ct);

        var content = new MultipartFormDataContent();
        var saveRequest = new SaveRequest
        {
            Key = key,
            FileName = fileName,
            MimeType = mimeType,
            AllowOverwrite = allowOverwrite,
            BaseUrl = ServerUrl
        };

        // Voeg JSON-velden als string toe
        content.Add(new StringContent(saveRequest.Key.ToString()), nameof(SaveRequest.Key));
        content.Add(new StringContent(saveRequest.FileName), nameof(SaveRequest.FileName));
        content.Add(new StringContent(saveRequest.MimeType), nameof(SaveRequest.MimeType));
        content.Add(new StringContent(saveRequest.BaseUrl), nameof(SaveRequest.BaseUrl));
        content.Add(new StringContent(saveRequest.AllowOverwrite ? "true" : "false"), nameof(SaveRequest.AllowOverwrite));

        // Voeg bestand toe
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "file", fileName);

        using var response = await HttpClient.PostAsync("/Storage/SaveStorageFile", content);
        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<SaveResponse>() 
            ?? throw new Exception("Could not cast response from file server");
        if (!model.Success)
            throw new Exception(model.ErrorMessage);
        if (model.Url == null)
            throw new Exception("Url is empty");
        if (!Uri.IsWellFormedUriString(model.Url, UriKind.Absolute))
            throw new Exception("getExternalUrlResponse.Url is not set or is not a valid URL. Please provide a valid server URL.");

        UrlCache[key] = new StorageFileCacheValue(model.Url, DateTime.GetUtcNow());
        return model.Url;
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
        await AuthenticateHttpClient(ct);

        var content = new MultipartFormDataContent();
        var saveRequest = new AppendRequest
        {
            Key = key,
            FileName = fileName,
            MimeType = mimeType,
            AllowCreate = allowCreate,
            BaseUrl = ServerUrl
        };

        // Voeg JSON-velden als string toe
        content.Add(new StringContent(saveRequest.Key.ToString()), nameof(AppendRequest.Key));
        content.Add(new StringContent(saveRequest.FileName), nameof(AppendRequest.FileName));
        content.Add(new StringContent(saveRequest.MimeType), nameof(AppendRequest.MimeType));
        content.Add(new StringContent(saveRequest.BaseUrl), nameof(AppendRequest.BaseUrl));
        content.Add(new StringContent(saveRequest.AllowCreate ? "true" : "false"), nameof(AppendRequest.AllowCreate));

        // Voeg bestand toe
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "file", fileName);

        using var response = await HttpClient.PostAsync("/Storage/AppendStorageFile", content);
        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<AppendResponse>()
            ?? throw new Exception("Could not cast response from file server");
        if (!model.Success)
            throw new Exception(model.ErrorMessage);
        if (model.Url == null)
            throw new Exception("Url is empty");
        if (!Uri.IsWellFormedUriString(model.Url, UriKind.Absolute))
            throw new Exception("getExternalUrlResponse.Url is not set or is not a valid URL. Please provide a valid server URL.");

        UrlCache[key] = new StorageFileCacheValue(model.Url, DateTime.GetUtcNow());
        return model.Url;
    }

    public async Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, CancellationToken ct, bool throwIfNotFound = false)
    {
        if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
            throw new ArgumentException(
                $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");

        await AuthenticateHttpClient(ct);

        var key = GetFileKey(storageFile);
        return await DeleteStorageFileAsync(key, ct, throwIfNotFound);
    }
    public async Task<bool> DeleteStorageFileAsync(string key, CancellationToken ct, bool throwIfNotFound = false)
    {
        var request = new DeleteRequest()
        {
            Key = key,
            BaseUrl = ServerUrl
        };

        using var responseMessage = await HttpClient.PostAsJsonAsync("/Storage/DeleteStorageFile", request, ct);
        responseMessage.EnsureSuccessStatusCode();

        var response = await responseMessage.Content.ReadFromJsonAsync<DeleteResponse>(ct)
            ?? throw new Exception("Could not cast response from file server");
        if (throwIfNotFound && response.Success == false)
            throw new Exception(response.ErrorMessage);

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

        using var response = await HttpClient.PostAsJsonAsync("/Auth/Login", loginRequest, ct);
        response.EnsureSuccessStatusCode();

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
        var jwtToken = (loginResult?.Token) ?? throw new Exception("Kan geen token ophalen");
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
    private StorageFileCacheValue UpdateUrlCache(string key)
    {
        var request = new GetStorageFileInfoRequest()
        {
            Key = key,
            BaseUrl = ServerUrl
        };

        using var response = HttpClient.PostAsJsonAsync("/Storage/GetStorageFileInfo", request).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var model = response.Content.ReadFromJsonAsync<GetStorageFileInfoResponse>().GetAwaiter().GetResult() ?? throw new Exception("Could not cast response from file server");
        if (!string.IsNullOrWhiteSpace(model.Url) && !Uri.IsWellFormedUriString(model.Url, UriKind.Absolute))
            throw new Exception("getExternalUrlResponse.Url is not set or is not a valid URL. Please provide a valid server URL.");

        return new(string.IsNullOrWhiteSpace(model.Url) ? null : model.Url, DateTime.GetUtcNow());
    }
}