using gAPI.Core.Server.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net;
using gAPI.Core.Server.Extensions;
using gAPI.Core.Extentions;

namespace gAPI.Core.Server;

public class AuthenticationMiddleware<TUser, TStateDto>
    where TUser : AuthUser
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(
        HttpContext ctx,
        IAuthenticationService<TUser, TStateDto> authentication,
        IHostEnvironment hostEnvironment)
    {
        IPAddress? forwardedIp = ctx.Connection.RemoteIpAddress;
        if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var ipHeader))
        {
            var firstIp = ipHeader.ToString().Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out var parsedIp))
                forwardedIp = parsedIp;
        }

        var path = ctx.Request.Path;
        var queryString = ctx.Request.QueryString;
        if (ctx.Request.Headers.TryGetValue("X-Forwarded-Uri", out var uriHeader))
        {
            if (Uri.TryCreate(new Uri("http://dummy"), uriHeader.ToString(), out var uri))
            {
                path = uri.AbsolutePath;
                queryString = new QueryString(uri.Query);
            }
        }

        var initResult = await authentication.InitializeAsync(
            path,
            queryString,
            forwardedIp,
            ctx.Request.Cookies["AuthenticationToken"],
            ctx.Request.Headers["X-SessionId"],
            ctx.Request.Headers["X-StateData"],
            ctx.RequestAborted);

        if (initResult.Forbidden)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            var response = hostEnvironment.IsDevelopment()
                ? initResult.ForbiddenReason ?? "Forbidden"
                : "Forbidden";
            await ctx.Response.WriteAsync(response, ctx.RequestAborted);
            return;
        }

        ctx.Response.OnStarting(async () =>
        {
            if (authentication.Initialized == false)
                return;

            var sessionId = authentication.SessionId.ToStringValues();
            var stateData = await authentication.GetStateDataAsync(ctx.RequestAborted);

            ctx.Response.Headers["X-SessionId"] = sessionId;
            ctx.Response.Headers["X-StateData"] = stateData;

            if (!authentication.UpdateCookie)
                return;

            if (ctx.Request.Cookies.TryGetValue("AuthenticationToken", out var _))
                ctx.Response.Cookies.Delete("AuthenticationToken");

            if (authentication.CookieData == null)
                return;

            ctx.Response.Cookies.Append(
                "AuthenticationToken",
                authentication.CookieData,
                new CookieOptions
                {
                    SameSite = hostEnvironment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
                    Secure = !hostEnvironment.IsDevelopment(),
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
        });

        await _next(ctx);
    }
}