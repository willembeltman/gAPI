using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gAPI.Core.Server.Authentication;

public class EmptyServerAuthenticationService
    : IServerAuthenticationService
{
    public UserId UserId { get; set; }

    public SessionId SessionId { get; set; }

    public string? SessionData { get; set; }

    public string? CookieData { get; set; }

    public bool UpdateCookie { get; set; }

    public bool Initialized { get; set; }

    public AuthenticationInitializeResult Result { get; set; } = new();

    public async Task<bool> AuthenticateUserAsync(string userId, CancellationToken ct)
    {
        return false;
    }

    public async Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct)
    {
        return new ClaimsPrincipal();
    }

    public async Task<string?> GetStateDataAsync(CancellationToken ct)
    {
        return null;
    }

    public async Task<AuthenticationInitializeResult> UpdateStateAsync(string? stateData, CancellationToken ct)
    {
        return new AuthenticationInitializeResult();
    }

    public async Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct)
    {
        return new AuthenticationInitializeResult();
    }

    public async Task<AuthenticationInitializeResult> InitializeAsync(string url, string? cookieData, string? sessionData, string? stateData, CancellationToken ct)
    {
        return new AuthenticationInitializeResult();
    }

    public async Task<AuthenticationInitializeResult> InitializeAsync(PathString path, QueryString query, IPAddress? ipAddress, string? cookieData, string? sessionData, string? stateData, CancellationToken ct)
    {
        return new AuthenticationInitializeResult();
    }

    public bool IsStateChanged()
    {
        return false;
    }

    public async Task<bool> LogoffAsync(CancellationToken ct)
    {
        return false;
    }
}
