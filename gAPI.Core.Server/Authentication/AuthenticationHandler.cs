using gAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace gAPI.Core.Server.Authentication;

public class AuthenticationHandler(
    IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServerAuthenticationService auth)
        : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        if (auth.Initialized == false)
            return Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult();

        if (auth.Result.Forbidden)
            return Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail(
                auth.Result.ForbiddenReason ?? "Forbidden");

        if (!auth.Result.Authenticated)
            return Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult();

        var principal = await auth.GetClaimsPrincipalAsync(Context.RequestAborted);
        return Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(
            new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "gAPI"));
    }
}