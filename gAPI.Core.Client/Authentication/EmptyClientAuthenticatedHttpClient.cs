//using gAPI.Core.Client.Interfaces;
//using gAPI.Core.Delegates;
//using gAPI.Core.Ids;
//using gAPI.Core.Sse;

//namespace gAPI.Core.Client.Authentication;

//public class EmptyClientAuthenticatedHttpClient : IClientAuthenticatedHttpClient
//{
//    public string StateData { get; set; } = string.Empty;
//    public SessionId SessionId { get; set; } = SessionId.New();
//    public UserId UserId { get; set; } = new UserId();
//    public Uri? BaseUri { get; set; }
//    public bool ForceReconnect { get; set; }

//    public event StateChangedHandler? OnStateHasChanged;

//    public async Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct)
//    {
//        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
//    }

//    public async Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct)
//    {
//        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
//    }

//    public async Task<string> GetStateDataAsync(bool force = false, CancellationToken ct = default)
//    {
//        return string.Empty;
//    }

//    public async Task<Stream> GetStreamAsync(string url, CancellationToken ct)
//    {
//        return Stream.Null;
//    }

//    public async Task<bool?> IsAuthenticatedAsync(CancellationToken ct = default)
//    {
//        return false;
//    }

//    public bool IsStateChanged()
//    {
//        return false;
//    }

//    public async Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken ct)
//    {
//        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
//    }

//    public async Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken ct)
//    {
//        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
//    }

//    public async Task TryUpdateStateAsync(string? stateData, CancellationToken ct)
//    {
//    }

//    public async Task TryUpdateStateAsync(ApiResult result, CancellationToken ct)
//    {
//    }

//    public async Task TryUpdateStateAsync(HttpResponseMessage response, CancellationToken ct)
//    {
//    }

//    public void Dispose()
//    {
//    }
//}
