using gAPI.Storage.StorageServer.Dtos.Requests;
using gAPI.Storage.StorageServer.Dtos.Responses;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

#nullable enable
namespace gAPI.Storage.StorageServer
{
    public class StorageServerService(IOptions<StorageServerConfig> config) : IStorageService
    {
        private async Task AuthenticateHttpClient(HttpClient httpClient, bool forceReload = false)
        {
            if (!forceReload && httpClient.DefaultRequestHeaders.Authorization != null)
                return;

            var credential = config.Value.Credential;
            if (credential == null || string.IsNullOrWhiteSpace(credential.UserName) || string.IsNullOrWhiteSpace(credential.Password))
                throw new Exception("StorageServerConfig.Credential is not set or incomplete. Please provide valid credentials.");

            if (config.Value.ServerUrl == null || !Uri.IsWellFormedUriString(config.Value.ServerUrl, UriKind.Absolute))
                throw new Exception("StorageServerConfig.ServerUrl is not set or is not a valid URL. Please provide a valid server URL.");

            if (string.IsNullOrWhiteSpace(credential.UserName) || credential.UserName == null)
                throw new Exception("credential.UserName is requered");
            if (string.IsNullOrWhiteSpace(credential.Password) || credential.Password == null)
                throw new Exception("credential.UserName is requered");

            httpClient.BaseAddress = new Uri(config.Value.ServerUrl);

            // Stap 1: Login
            var loginRequest = new LoginRequest
            {
                Username = credential.UserName,
                Password = credential.Password
            };

            using var response = await httpClient.PostAsJsonAsync("/Auth/Login", loginRequest);
            response.EnsureSuccessStatusCode();

            var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
            var jwtToken = loginResult?.Token;
            if (jwtToken == null) throw new Exception("Kan geen token ophalen");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        }

        public async Task<string?> GetStorageFileUrlAsync(IStorageFile storageFile)
        {
            return await GetStorageFileUrlAsync(storageFile.Id, storageFile.GetType().Name);
        }
        public async Task<string?> GetStorageFileUrlAsync(string? id, string type)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "0" || id == null)
                throw new ArgumentException(
                    $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");

            if (string.IsNullOrWhiteSpace(config.Value.ServerUrl) || config.Value.ServerUrl == null)
                throw new ArgumentException("config.Value.ServerUrl is nullllllll waarom moet ik dit zelf schrijven chatgpt???");

            using var httpClient = new HttpClient();
            await AuthenticateHttpClient(httpClient);

            var request = new GetStorageFileInfoRequest()
            {
                Id = id,
                TypeName = type,
                BaseUrl = config.Value.ServerUrl
            };

            using var response = await httpClient.PostAsJsonAsync("/Storage/GetStorageFileInfo", request);
            response.EnsureSuccessStatusCode();

            var model = await response.Content.ReadFromJsonAsync<GetStorageFileInfoResponse>();
            if (model == null)
                throw new Exception("Could not cast response from file server");
            if (!string.IsNullOrWhiteSpace(model.Url) && !Uri.IsWellFormedUriString(model.Url, UriKind.Absolute))
                throw new Exception("getExternalUrlResponse.Url is not set or is not a valid URL. Please provide a valid server URL.");

            return string.IsNullOrWhiteSpace(model.Url) ? null : model.Url;
        }

        public async Task<string?> SaveStorageFileAsync(IStorageFile storageFile, string fileName, string mimeType, Stream stream, bool allowOverwrite = true)
        {
            if (config.Value.ServerUrl == null)
                throw new Exception("StorageServerConfig.ServerUrl is not set. Please provide a valid server URL.");
            if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
                throw new ArgumentException(
                    $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(
                    $"Cannot use storage file server for entities without StorageFileName filled.");
            if (string.IsNullOrWhiteSpace(mimeType))
                throw new ArgumentException(
                    $"Cannot use storage file server for entities without StorageMimeType filled.");

            using var httpClient = new HttpClient();

            await AuthenticateHttpClient(httpClient);

            var content = new MultipartFormDataContent();
            var saveRequest = new SaveRequest
            {
                Id = storageFile.Id,
                FileName = fileName,
                TypeName = storageFile.GetType().Name,
                MimeType = mimeType,
                AllowOverwrite = allowOverwrite,
                BaseUrl = config.Value.ServerUrl
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

            using var response = await httpClient.PostAsync("/Storage/SaveStorageFile", content);
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

            return model.Url;
        }

        public async Task<bool> DeleteStorageFileAsync(IStorageFile storageFile, bool throwIfNotFound)
        {
            if (string.IsNullOrWhiteSpace(storageFile.Id) || storageFile.Id == "0")
                throw new ArgumentException(
                    $"Cannot use storage file server for entities with Id = 0, this indicates the entity has not been attached to the dbcontext jet.");

            if (string.IsNullOrWhiteSpace(config.Value.ServerUrl) || config.Value.ServerUrl == null)
                throw new ArgumentException(
                    "config.Value.ServerUrl is null slimmie");

            using var httpClient = new HttpClient();
            await AuthenticateHttpClient(httpClient);

            var request = new DeleteRequest()
            {
                Id = storageFile.Id,
                TypeName = storageFile.GetType().Name,
                BaseUrl = config.Value.ServerUrl
            };

            using var responseMessage = await httpClient.PostAsJsonAsync("/Storage/DeleteStorageFile", request);
            responseMessage.EnsureSuccessStatusCode();

            var response = await responseMessage.Content.ReadFromJsonAsync<DeleteResponse>();
            if (response == null)
                throw new Exception("Could not cast response from file server");
            if (throwIfNotFound && response.Success == false)
                throw new Exception(response.Message);

            return response.Deleted;
        }

    }
}