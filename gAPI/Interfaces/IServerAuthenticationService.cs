using gAPI.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace gAPI.Interfaces;

public interface IServerAuthenticationService
{
    AuthenticationHeaders Headers { get; }
    string SessionId { get; }
    string? UserId { get; }
    Task<AuthenticationInitializeResult> InitializeAsync(PathString path, QueryString query, string? cookieData, StringValues sessionData, StringValues stateData);
    Task<AuthenticationInitializeResult> ReInitializeAsync();
    Task<ClaimsPrincipal> GetClaimsPrincipalAsync();
}
