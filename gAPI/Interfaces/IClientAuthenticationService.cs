using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace gAPI.Interfaces
{
    public interface IClientAuthenticationService
    {
        Guid ScopeIdentifier { get; }
        Task<bool> IsAuthenticated();
        Task<HttpResponseMessage> GetAsync(string path);
        Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content);
        Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content);
        Task<HttpResponseMessage> DeleteAsync(string path);
        Task AfterReceivedResponseIsParsedAsync(object response);
    }
}