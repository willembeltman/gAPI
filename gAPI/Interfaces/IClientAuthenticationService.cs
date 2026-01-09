using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace gAPI.Interfaces;

public interface IClientAuthenticationService
{
    Guid SessionId { get; }
    Task<bool> IsAuthenticated(CancellationToken? ct = null);
    Task<Stream> GetStreamAsync(string url, CancellationToken ct);
    Task<HttpResponseMessage> GetAsync(string path, CancellationToken? ct = null, HttpCompletionOption? option = null);
    Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken? ct = null);
    Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken? ct = null);
    Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken? ct = null);
    Task AfterReceivedResponseIsParsedAsync(object response, CancellationToken? ct = null);
}