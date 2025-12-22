using gAPI.Interfaces;

namespace gAPI.Test.Api.Services
{
    public class ServerAuthenticationService : IServerAuthenticationService
    {
        public async Task<string?> GetUserId()
        {
            return await Task.Run(() =>
            {
                return "Test";
            });
        }

        public async Task<bool> InitializeAsync(Guid scopeIdentifier, string? bearerToken = null)
        {
            return await Task.Run(() =>
            {
                return true;
            });
        }
    }
}
