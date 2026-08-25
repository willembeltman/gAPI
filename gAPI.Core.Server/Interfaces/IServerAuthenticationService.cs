using gAPI.Core.Ids;
using gAPI.Core.Server.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gAPI.Core.Interfaces;

public interface IServerAuthenticationService
{
    UserId UserId { get; }
    SessionId SessionId { get; }
    string? SessionData { get; }
    string? CookieData { get; }
    bool UpdateCookie { get; }
    bool Initialized { get; }
    AuthenticationInitializeResult Result { get; }

    Task<AuthenticationInitializeResult> InitializeAsync(string url, string? cookieData, string? sessionData, string? stateData, CancellationToken ct);
    Task<AuthenticationInitializeResult> InitializeAsync(PathString path, QueryString query, IPAddress? ipAddress, string? cookieData, string? sessionData, string? stateData, CancellationToken ct);
    Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct);

    bool IsStateChanged();
    Task<string?> GetStateDataAsync(CancellationToken ct);
    Task<AuthenticationInitializeResult> UpdateStateDataAsync(string? stateData, CancellationToken ct);


    Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct);
    Task<bool> AuthenticateUserAsync(string userId, CancellationToken ct);
    Task<bool> LogoffAsync(CancellationToken ct);
}
