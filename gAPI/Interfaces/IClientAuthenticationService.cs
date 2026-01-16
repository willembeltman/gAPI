namespace gAPI.Interfaces;

public interface IClientAuthenticationService
{
    string SessionId { get; }
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
    Task<Stream> GetStreamAsync(string url, CancellationToken ct = default);
    Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken ct = default);
    Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct = default);
}