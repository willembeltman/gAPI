using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace gAPI.Interfaces
{
    public interface IClientAuthenticationService
    {
        Guid SessionId { get; }
        Task<bool> IsAuthenticated(CancellationToken? token = null);
        Task<Stream> GetStreamAsync(string url, CancellationToken? token = null);
        Task<HttpResponseMessage> GetAsync(string path, CancellationToken? token = null, HttpCompletionOption? option = null);
        Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken? token = null);
        Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken? token = null);
        Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken? token = null);
        Task AfterReceivedResponseIsParsedAsync(object response, CancellationToken? token = null);
    }
}