using gAPI.Delegates;
using gAPI.Ids;
using gAPI.Sse;

namespace gAPI.Interfaces;

public interface IClientAuthenticatedHttpClient : IDisposable
{
    event StateChangedHandler? OnStateHasChanged;

    SessionId SessionId { get; }
    UserId UserId { get; }
    Uri? BaseUri { get; }
    bool ForceReconnect { get; set; }

    Task<string> GetStateDataAsync(bool force = false, CancellationToken ct = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
    Task TryUpdateStateAsync(string? stateData, CancellationToken ct);
    Task TryUpdateStateAsync(ApiResult result, CancellationToken ct);
    Task TryUpdateStateAsync(HttpResponseMessage response, CancellationToken ct);
    bool IsStateChanged();

    Task<Stream> GetStreamAsync(string url, CancellationToken ct);
    Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct);
    Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken ct);
    Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken ct);
    Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct);
}