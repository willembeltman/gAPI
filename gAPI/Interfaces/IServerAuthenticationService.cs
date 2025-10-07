using System;
using System.Threading.Tasks;

namespace gAPI.Interfaces
{
    public interface IServerAuthenticationService
    {
        /// <summary>
        /// Is called to see if the request is authenticated. Return true if client is authenticated, otherwise return false.
        /// </summary>
        /// <param name="scopeIdentifier"></param>
        /// <returns></returns>
        Task<bool> InitializeAsync(Guid scopeIdentifier, string? bearerToken = null);
    }
}