using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Collections;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Security.Claims;

namespace gAPI.Core.Server.Authentication;

public class AuthenticationService<TUser, TStateDto>(
    IAuthenticationStateFactory<TUser> authenticationStateFactory,
    IUserTokenFactory<TUser> userTokenFactory,
    IStateMapping<TUser, TStateDto> stateMapping,
    IStateParser<TStateDto> stateParser,
    IHostEnvironment hostEnvironment,
    IEnumerable<IAuthenticationCheck<TUser, TStateDto>> authenticationChecks, // optioneel dus.
    IEnumerable<WssSessionCache> sessionCaches) // optioneel dus.
    : IAuthenticationService<TUser, TStateDto>
    where TUser : AuthUser
    where TStateDto : AuthStateDto, new()
{
    private AuthenticationHeaders? _Headers;
    private TStateDto? _ReceivedClientState;
    private TStateDto? _OldState;
    private TStateDto? _State;
    private AuthenticationState<TUser>? _AuthenticationState { get; set; }

    private AuthenticationInitializeResult? _Result;

    public bool Initialized { get; private set; }

    public AuthenticationInitializeResult Result
        => _Result ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    public TStateDto? ClientState
        => _ReceivedClientState;
    public TStateDto State
        => _State ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    public AuthenticationState<TUser> AuthenticationState
        => _AuthenticationState ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    public SessionId SessionId
        => _Headers?.SessionId ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    public UserId UserId
        => new(_AuthenticationState?.User?.Id.ToString());

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
            _Result = new AuthenticationInitializeResult()
            {
                Forbidden = true,
                ForbiddenReason = "No IP address found."
            };
            return _Result;
        }

        if (hostEnvironment.IsDevelopment() &&
            (path.ToString().StartsWith("/scalar", StringComparison.CurrentCultureIgnoreCase) ||
            path.ToString().StartsWith("/openapi", StringComparison.CurrentCultureIgnoreCase)))
        {
            _Result = new AuthenticationInitializeResult();
            return _Result;
        }

        if (sessionData == null)
        {
            _Result = new AuthenticationInitializeResult()
            {
                Forbidden = true,
                ForbiddenReason = "No session data found."
            };
            return _Result;
        }
        var sessionId = new SessionId(sessionData);

        _Headers = new AuthenticationHeaders(path, query, ipAddress, cookieData, stateData, sessionId);
        return await Make(_Headers, ct);
    }
    public async Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct)
    {
        if (_AuthenticationState == null || _Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        return await Make(_Headers, ct);
    }
    private async Task<AuthenticationInitializeResult> Make(AuthenticationHeaders headers, CancellationToken ct)
    {
        if (stateParser.TryParse(headers.StateData, out var recievedClientState))
        {
            _ReceivedClientState = recievedClientState;
        }

        _AuthenticationState = await authenticationStateFactory.CreateAuthenticationStateAsync(headers, ct);
        _State = await stateMapping.ToDtoAsync(_AuthenticationState.User, _AuthenticationState.Token, _AuthenticationState.Ip, _ReceivedClientState, ct);
        _OldState = stateParser.CreateCopy(_State);

        // Check lockout
        if (_AuthenticationState.User?.LockedOut == true)
        {
            _Result = new AuthenticationInitializeResult()
            {
                Forbidden = true,
                ForbiddenReason = "User is locked out"
            };
            return _Result;
        }

        // Additional forbidden checks
        foreach (var check in authenticationChecks)
        {
            if (!check.IsValid(headers, _ReceivedClientState, _State, _AuthenticationState, out AuthenticationInitializeResult notValidResult))
            {
                return notValidResult;
            }
        }

        Initialized = true;
        foreach (var sessionCache in sessionCaches)
            sessionCache.AddOrUpdate(headers.SessionId, headers.CookieData);

        _Result = new AuthenticationInitializeResult()
        {
            Authenticated = _AuthenticationState.User != null,
        };
        return _Result;
    }

    public bool IsStateDataChanged()
    {
        if (stateParser.IsDifferent(_OldState, _State))
        {
            _OldState = stateParser.CreateCopy(_State);
            return true;
        }
        return false;
    }
    public string? GetStateData()
    {
        if (_State == null || _Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");
        return stateParser.ToStringBase64(_State);
    }
    public async Task<AuthenticationInitializeResult> UpdateStateDataAsync(string? stateData, CancellationToken ct)
    {
        if (_AuthenticationState == null || _Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        if (stateData == null)
            return _Result ?? throw new Exception("Initialize the ServerAuthenticationService first please");

        _Headers.StateData = stateData;

        return await Make(_Headers, ct);
    }

    public bool IsCookieDataChanged()
    {
        return _Headers?.UpdateCookie ?? throw new Exception("Initialize the ServerAuthenticationService first please");
    }
    public string? GetCookieData()
    {
        if (_State == null || _Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");
        return _Headers?.CookieData;
    }

    public async Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct)
    {
        if (_AuthenticationState == null || _Headers == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        if (_AuthenticationState.User == null)
            throw new Exception("User is not authenticated");

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, _AuthenticationState.User.Id.ToString()),
            new Claim(ClaimTypes.Name, _AuthenticationState.User.Email),
            new Claim("UserId", _AuthenticationState.User.Id.ToString())
        ];

        var identity = new ClaimsIdentity(claims, authenticationType: "Cookie");
        var user = new ClaimsPrincipal(identity);
        return user;
    }
    public async Task<bool> AuthenticateUserAsync(string userId, CancellationToken ct)
    {
        if (_AuthenticationState == null || _Headers == null)
            return false;
        //throw new Exception("Initialize the ServerAuthenticationService first please");

        if (userId == null)
            return false;
        //throw new Exception("UserId not valid user seems to not be selected");

        // Sets cookie data in Headers, and gets new cookie hash
        var cookieHash = _Headers.CreateNewCookie();

        // Save token
        await userTokenFactory.SaveTokenAsync(userId, cookieHash, ct);

        // Re-initialize using old header, with new cookie data
        var initResult = await ReInitializeAsync(ct);
        if (initResult.Forbidden)
            return false;
        //throw new Exception($"User forbidden: {initResult.ForbiddenReason}");

        return true;
    }
    public async Task<bool> LogoffAsync(CancellationToken ct)
    {
        if (_AuthenticationState == null || _Headers == null)
            return false;
        //throw new Exception("Initialize the ServerAuthenticationService first please");

        foreach (var sessionCache in sessionCaches)
            sessionCache.Remove(_Headers.SessionId);
        _Headers.RemoveCookie();
        await ReInitializeAsync(ct);

        return _AuthenticationState == null || _AuthenticationState.User == null;
    }

}