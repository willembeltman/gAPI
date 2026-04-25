using gAPI.Authentication;
using gAPI.Ids;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Security.Claims;

namespace gAPI.Interfaces;

public interface IServerAuthenticationService
{
    UserId UserId { get; }
    SessionId SessionId { get; }
    StringValues SessionData { get; }
    string? CookieData { get; }
    bool UpdateCookie { get; }
    bool Initialized { get; }
    AuthenticationInitializeResult Result { get; }

    Task<AuthenticationInitializeResult> InitializeAsync(PathString path, QueryString query, IPAddress? ipAddress, string? cookieData, string? sessionData, string? stateData, CancellationToken ct);
    Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct);
    Task<AuthenticationInitializeResult> UpdateStateAsync(string? stateData, CancellationToken ct);
    Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct);
    Task<string?> GetStateDataAsync(CancellationToken ct);
    bool IsStateChanged();
    Task<bool> AuthenticateUserAsync(string userId, CancellationToken ct);
    Task<bool> LogoffAsync(CancellationToken ct);
}
