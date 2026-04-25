using gAPI.Core.Server.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace gAPI.Core.Server;

public class AuthenticationHandler<TUser, TStateDto>
    : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
    where TUser : AuthUser
{
    private readonly IAuthenticationService<TUser, TStateDto> Authentication;

    public AuthenticationHandler(
        IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TimeProvider timeProvider,
        IAuthenticationService<TUser, TStateDto> auth)
        : base(options, logger, encoder)
    {
        Authentication = auth;
    }

    protected override async Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Authentication.Initialized == false)
            return Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult();

        if (Authentication.Result.Forbidden)
            return Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail(
                Authentication.Result.ForbiddenReason ?? "Forbidden");

        if (!Authentication.Result.Authenticated)
            return Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult();

        var principal = await Authentication.GetClaimsPrincipalAsync(Context.RequestAborted);
        return Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(
            new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "gAPI"));
    }
}