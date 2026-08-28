using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace gAPI.Core.Server.Authentication;

public class EmptyServerAuthenticationService<TStateDto>
    : IServerAuthenticationService
{
    public UserId UserId { get; set; } = new UserId();

    public SessionId SessionId { get; set; } = SessionId.New();

    public bool Initialized { get; set; } = true;

    public AuthenticationInitializeResult Result { get; set; } = new();

    public async Task<AuthenticationInitializeResult> InitializeAsync(
        PathString path, 
        QueryString query,
        IPAddress? ipAddress,
        string? cookieId,
        string? sessionId, 
        string? stateData,
        bool updateSession,
        CancellationToken ct)
    {
        if (SessionId.TryParse(sessionId, out var parsed))
            SessionId = parsed;
        Initialized = true;
        return new AuthenticationInitializeResult();
    }
    public async Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct)
    {
        return new AuthenticationInitializeResult();
    }

    public bool IsStateDataChanged()
    {
        return false;
    }
    public string? GetStateData()
    {
        return null;
    }
    public async Task<AuthenticationInitializeResult> UpdateStateDataAsync(string? stateData, CancellationToken ct)
    {
        return new AuthenticationInitializeResult();
    }

    public bool IsCookieDataChanged()
    {
        return false;
    }
    public string? GetCookieData()
    {
        return null;
    }

    public async Task<bool> AuthenticateUserAsync(string userId, CancellationToken ct)
    {
        return false;
    }
    public async Task<bool> LogoffAsync(CancellationToken ct)
    {
        return false;
    }

    public async Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct)
    {
        return new ClaimsPrincipal();
    }

}
