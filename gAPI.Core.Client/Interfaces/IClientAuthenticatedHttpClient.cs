using gAPI.Core.Delegates;
using gAPI.Core.Ids;

namespace gAPI.Core.Client.Interfaces;

public interface IClientAuthenticatedHttpClient : IDisposable
{
    event StateChangedHandler? OnStateHasChanged;

    SessionId SessionId { get; }
    UserId UserId { get; }
    Uri? BaseUri { get; }
    bool ForceReconnect { get; set; }

    Task<bool?> IsAuthenticatedAsync(CancellationToken ct = default);

    bool IsStateDataChanged();
    Task<string> GetStateDataAsync(bool force = false, CancellationToken ct = default);
    Task UpdateStateDataAsync(string? stateData, CancellationToken ct);

    Task<Stream> GetStreamAsync(string url, CancellationToken ct);
    Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct);
    Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken ct);
    Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken ct);
    Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct);
}