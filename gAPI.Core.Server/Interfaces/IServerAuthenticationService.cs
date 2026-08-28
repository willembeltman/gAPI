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
    bool Initialized { get; }
    AuthenticationInitializeResult Result { get; }

    Task<AuthenticationInitializeResult> InitializeAsync(
        PathString path, 
        QueryString query,
        IPAddress? ipAddress,
        string? cookieData, 
        string? sessionData,
        string? stateData,
        bool updateSession,
        CancellationToken ct);
    Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct);

    bool IsStateDataChanged();
    string? GetStateData();
    Task<AuthenticationInitializeResult> UpdateStateDataAsync(string? stateData, CancellationToken ct);

    bool IsCookieDataChanged();
    string? GetCookieData();

    Task<bool> AuthenticateUserAsync(string userId, CancellationToken ct);
    Task<bool> LogoffAsync(CancellationToken ct);
    Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct);
}
