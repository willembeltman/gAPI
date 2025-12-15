using gAPI.Interfaces;

namespace gAPI.Test.Backend.Services
{
    public class ServerAuthenticationService : IServerAuthenticationService
    {
        public async Task<bool> InitializeAsync(Guid scopeIdentifier, string? bearerToken = null)
        {
            return await Task.Run(() => { return true; });
        }
    }
}
