using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Security.Claims;

namespace gAPI.Core.Server.Authentication;

public class AuthenticationService<TUser, TStateDto>(
    IAuthenticationStateFactory<TUser, TStateDto> factory,
    IStateParser<TStateDto> stateSerializer,
    IHostEnvironment hostEnvironment,
    IEnumerable<WssSessionCache> sessionCaches) // optioneel dus.
    : IAuthenticationService<TUser, TStateDto>
    where TUser : AuthUser
    where TStateDto : AuthStateDto, new()
{
    private AuthenticationHeaders? Headers;

    private TStateDto? ReceivedClientState;
    private TStateDto? OldState;
    private TStateDto? State;
    private AuthenticationState<TUser>? AuthenticationState { get; set; }

    private AuthenticationInitializeResult? Result;
    public bool Initialized { get; private set; }

    AuthenticationInitializeResult IServerAuthenticationService.Result
        => Result ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    TStateDto? IAuthenticationService<TUser, TStateDto>.ClientState
        => ReceivedClientState;
    TStateDto IAuthenticationService<TUser, TStateDto>.State
        => State ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    AuthenticationState<TUser> IAuthenticationService<TUser, TStateDto>.AuthenticationState
        => AuthenticationState ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    SessionId IServerAuthenticationService.SessionId
        => Headers?.SessionId ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    UserId IServerAuthenticationService.UserId
        => new(AuthenticationState?.User?.Id.ToString());

    public Task<AuthenticationInitializeResult> InitializeAsync(string url, string? cookieData, string? sessionData, string? stateData, CancellationToken ct)
    {
        return InitializeAsync(
            new Microsoft.AspNetCore.Http.PathString("/"),
            new Microsoft.AspNetCore.Http.QueryString(),
            System.Net.IPAddress.Loopback,
            cookieData,
            sessionData,
            stateData,
            ct);
    }
    public async Task<AuthenticationInitializeResult> InitializeAsync(PathString path, QueryString query, IPAddress? ipAddress, string? cookieData, string? sessionData, string? stateData, CancellationToken ct)
    {
        if (ipAddress == null)
        {
            Result = new AuthenticationInitializeResult()
            {
                Forbidden = true,
                ForbiddenReason = "No IP address found."
            };
            return Result;
        }

        if (hostEnvironment.IsDevelopment() &&
            (path.ToString().StartsWith("/scalar", StringComparison.CurrentCultureIgnoreCase) ||
            path.ToString().StartsWith("/openapi", StringComparison.CurrentCultureIgnoreCase)))
        {
            Result = new AuthenticationInitializeResult();
            return Result;
        }

        if (sessionData == null)
        {
            Result = new AuthenticationInitializeResult()
            {
                Forbidden = true,
                ForbiddenReason = "No session data found."
            };
            return Result;
        }
        var sessionId = new SessionId(sessionData);

        Headers = new AuthenticationHeaders(path, query, ipAddress, cookieData, stateData, sessionId);
        return await Make(Headers, ct);
    }
    public async Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct)
    {
        if (AuthenticationState == null || Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        return await Make(Headers, ct);
    }
    private async Task<AuthenticationInitializeResult> Make(AuthenticationHeaders headers, CancellationToken ct)
    {
        if (stateSerializer.TryParse(headers.StateData, out var recievedClientState))
        {
            ReceivedClientState = recievedClientState;
        }
        (State, AuthenticationState) = await factory.CreateAuthenticationStateAsync(headers, ReceivedClientState, ct);
        OldState = stateSerializer.CreateCopy(State);

        if (AuthenticationState.User?.LockedOut == true)
        {
            Result = new AuthenticationInitializeResult()
            {
                Forbidden = true,
                ForbiddenReason = "User is locked out"
            };
            return Result;
        }
        // Additional forbidden checks can be added here

        Initialized = true;
        foreach (var sessionCache in sessionCaches)
            sessionCache.AddOrUpdate(headers.SessionId, headers.CookieData);

        Result = new AuthenticationInitializeResult()
        {
            Authenticated = AuthenticationState.User != null,
        };
        return Result;
    }

    public bool IsStateDataChanged()
    {
        if (stateSerializer.IsDifferent(OldState, State))
        {
            OldState = stateSerializer.CreateCopy(State);
            return true;
        }
        return false;
    }
    public string? GetStateData()
    {
        if (State == null || Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");
        return stateSerializer.ToStringBase64(State);
    }
    public async Task<AuthenticationInitializeResult> UpdateStateDataAsync(string? stateData, CancellationToken ct)
    {
        if (AuthenticationState == null || Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        if (stateData == null)
            return Result ?? throw new Exception("Initialize the ServerAuthenticationService first please");

        Headers.StateData = stateData;

        return await Make(Headers, ct);
    }
    

    //bool IServerAuthenticationService.UpdateCookie
    //    => Headers?.UpdateCookie ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    //string? IServerAuthenticationService.CookieData
    //    => Headers?.CookieData;
    public bool IsCookieDataChanged()
    {
        return Headers?.UpdateCookie ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    }
    public string? GetCookieData()
    {
        if (State == null || Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");
        return Headers?.CookieData;
    }

    public async Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct)
    {
        if (AuthenticationState == null || Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        if (AuthenticationState.User == null)
            throw new Exception("User is not authenticated");

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, AuthenticationState.User.Id.ToString()),
            new Claim(ClaimTypes.Name, AuthenticationState.User.Email),
            new Claim("UserId", AuthenticationState.User.Id.ToString())
        ];

        var identity = new ClaimsIdentity(claims, authenticationType: "Cookie");
        var user = new ClaimsPrincipal(identity);
        return user;
    }
    public async Task<bool> AuthenticateUserAsync(string userId, CancellationToken ct)
    {
        if (AuthenticationState == null || Headers == null)
            return false;
        //throw new Exception("Initialize the ServerAuthenticationService first please");

        if (userId == null)
            return false;
        //throw new Exception("UserId not valid user seems to not be selected");

        // Sets cookie data in Headers, and gets new cookie hash
        var cookieHash = Headers.CreateNewCookie();

        // Save token
        await factory.SaveTokenAsync(userId, cookieHash, ct);

        // Re-initialize using old header, with new cookie data
        var initResult = await ReInitializeAsync(ct);
        if (initResult.Forbidden)
            return false;
        //throw new Exception($"User forbidden: {initResult.ForbiddenReason}");

        return true;
    }
    public async Task<bool> LogoffAsync(CancellationToken ct)
    {
        if (AuthenticationState == null || Headers == null)
            return false;
        //throw new Exception("Initialize the ServerAuthenticationService first please");

        foreach (var sessionCache in sessionCaches)
            sessionCache.Remove(Headers.SessionId);
        Headers.RemoveCookie();
        await ReInitializeAsync(ct);

        return AuthenticationState == null || AuthenticationState.User == null;
    }

}