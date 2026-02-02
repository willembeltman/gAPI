using gAPI.Authentication;
using gAPI.Interfaces;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Security.Claims;

namespace gAPI.Test.Api.Services
{
    public class ServerAuthenticationService : IServerAuthenticationService
    {
        public string SessionId => throw new NotImplementedException();

        public string? UserId => throw new NotImplementedException();

        public string? CookieData => throw new NotImplementedException();

        public bool UpdateCookie => throw new NotImplementedException();

        public AuthenticationInitializeResult Result => throw new NotImplementedException();

        public Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<StringValues> GetStateData(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

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

        public Task<AuthenticationInitializeResult> InitializeAsync(PathString path, QueryString query, IPAddress? ipAddress, string? cookieData, StringValues sessionData, StringValues stateData, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
