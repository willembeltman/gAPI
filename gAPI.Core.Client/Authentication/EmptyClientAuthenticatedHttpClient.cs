using gAPI.Core.Client.Interfaces;
using gAPI.Core.Delegates;
using gAPI.Core.Ids;
using gAPI.Core.Sse;

namespace gAPI.Core.Client.Authentication;

public class EmptyClientAuthenticatedHttpClient : IClientAuthenticatedHttpClient
{
    public string StateData { get; set; }
    public SessionId SessionId { get; set; }
    public UserId UserId { get; set; }
    public Uri? BaseUri { get; set; }
    public bool ForceReconnect { get; set; }

    public event StateChangedHandler? OnStateHasChanged;

    public Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetStateDataAsync(bool force = false, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> GetStreamAsync(string url, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<bool?> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public bool IsStateChanged()
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task TryUpdateStateAsync(string? stateData, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task TryUpdateStateAsync(ApiResult result, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task TryUpdateStateAsync(HttpResponseMessage response, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
