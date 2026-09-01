using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Security.Claims;

namespace gAPI.Core.Server.Authentication;

public class NoDbServerAuthenticationService<TUser, TStateDto>(
    IStateMapping<TUser, TStateDto> stateMapping,
    IStateParser<TStateDto> stateParser,
    FabricClient fabricClient,
    AuthenticationOptions authenticationOptions,
    IEnumerable<IAuthenticationCheck<TUser, TStateDto>> authenticationChecks)
    : IAuthenticationService<TUser, TStateDto>
    where TUser : AuthUser
    where TStateDto : AuthStateDto, new()
{
    private AuthenticationInitializeResult? _Result;
    private TStateDto? _ClientState;
    private TStateDto? _State;
    private TStateDto? _OldState;

    public UserId UserId { get; set; } = new();
    public SessionId SessionId { get; set; } = SessionId.New();

    public bool Initialized => _Result != null;

    public AuthenticationInitializeResult Result => _Result ?? throw new Exception("Please initialize the NoDbServerAuthenticationService first");
    public TStateDto? ClientState => _ClientState;
    public TStateDto State => _State ?? throw new Exception("Please initialize the NoDbServerAuthenticationService first");
    public AuthenticationState<TUser> AuthenticationState => throw new Exception("Please do not use in no-db mode");

    public async Task<AuthenticationInitializeResult> InitializeAsync(
        PathString path,
        QueryString query,
        IPAddress? ipAddress,
        string? cookieData,
        string? sessionId,
        string? stateData,
        CancellationToken ct)
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

        if (SessionId.TryParse(sessionId, out var parsedSessionId))
            SessionId = parsedSessionId;

        _ClientState = null;
        if (stateParser.TryParse(stateData, out var clientState))
            _ClientState = clientState;

        _OldState = stateParser.CreateCopy(_ClientState);

        _State = await stateMapping.ToDtoAsync(null, null, null, _ClientState, ct);

        if (authenticationOptions.UpdateSession)
            await fabricClient.UpdateSession(parsedSessionId, cookieData, ct);

        _Result = new AuthenticationInitializeResult();
        return Result;
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
        if (!Initialized)
            throw new Exception(
                "Initialize the NoDbServerAuthenticationService first please");

        return stateParser.ToStringBase64(_State);
    }
    public async Task<AuthenticationInitializeResult> UpdateStateDataAsync(
        string? stateData,
        CancellationToken ct)
    {
        if (!Initialized)
            throw new Exception(
                "Initialize the NoDbServerAuthenticationService first please");

        if (stateParser.TryParse(stateData, out var state))
        {
            _State = await stateMapping.ToDtoAsync(null, null, null, state, ct);
        }

        return Result;
    }

    public bool IsCookieDataChanged()
    {
        return false;
    }
    public string? GetCookieData()
    {
        return null;
    }

    public Task<bool> AuthenticateUserAsync(
        string userId,
        CancellationToken ct)
    {
        // NoDb heeft bewust geen user/authentication.
        return Task.FromResult(false);
    }

    public Task<bool> LogoffAsync(CancellationToken ct)
    {
        // Er is geen authentication state om uit te loggen.
        return Task.FromResult(true);
    }

    public Task<ClaimsPrincipal> GetClaimsPrincipalAsync(
        CancellationToken ct)
    {
        // NoDb is nooit authenticated.
        return Task.FromResult(new ClaimsPrincipal());
    }
}
