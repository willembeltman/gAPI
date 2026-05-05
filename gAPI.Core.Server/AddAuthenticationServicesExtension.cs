using gAPI.Core.Server.Entities;
using gAPI.Core.Dtos;
using gAPI.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System.Net;
using gAPI.Core.Server.Authentication;

namespace gAPI.Core.Server;

public static partial class AddAuthenticationServicesExtension
{
    public static IServiceCollection AddAuthenticationServices<TUser, TStateDto>(this IServiceCollection services)
        where TUser : AuthUser, new()
        where TStateDto : AuthStateDto, new()
    {
        services.AddScoped<IAccountService, AccountService<TUser, TStateDto>>();

        services.AddScoped<ServerAuthenticationAccessor>();
        services.AddScoped<AuthenticationService<TUser, TStateDto>>();
        services.AddScoped(sp =>
        {
            var accessor = sp.GetRequiredService<ServerAuthenticationAccessor>();

            if (accessor.Current is null)
                return sp.GetRequiredService<AuthenticationService<TUser, TStateDto>>();

            return (accessor.Current as IAuthenticationService<TUser, TStateDto>)!;
        });
        services.AddScoped(sp =>
        {
            var accessor = sp.GetRequiredService<ServerAuthenticationAccessor>();

            if (accessor.Current is null)
                return sp.GetRequiredService<AuthenticationService<TUser, TStateDto>>();

            return (accessor.Current as IServerAuthenticationService)!;
        });
        services.AddAuthentication("gAPI")
                .AddScheme<AuthenticationSchemeOptions, AuthenticationHandler<TUser, TStateDto>>("gAPI", _ => { });
        services.AddScoped<IAuthenticationStateFactory<TUser, TStateDto>, AuthenticationStateFactory<TUser, TStateDto>>();
        services.AddScoped<IAuthenticationSecurity, AuthenticationSecurity<TUser, TStateDto>>();

        return services;
    }

    public static WebApplication MapStateEndpoint_ForNoMiddleware<TUser, TStateDto>(this WebApplication app)
        where TUser : AuthUser, new()
        where TStateDto : AuthStateDto, new()
    {
        app.MapGet("/__state", async (
            CancellationToken ct,
            HttpContext ctx,
            [FromHeader(Name = "X-SessionId")] string sessionId,
            [FromHeader(Name = "X-StateData")] string? stateData,
            [FromServices] IServerAuthenticationService authenticationService,
            [FromServices] IHostEnvironment hostEnvironment) =>
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

            var initializeResult = await authenticationService.InitializeAsync(
                path,
                queryString,
                forwardedIp,
                ctx.Request.Cookies["AuthenticationToken"],
                new StringValues(sessionId),
                new StringValues(stateData),
                ct);
            if (initializeResult.Forbidden == true) return Results.Forbid();

            ctx.Response.Headers["X-SessionId"] = authenticationService.SessionData;
            ctx.Response.Headers["X-StateData"] = await authenticationService.GetStateDataAsync(ct);
            if (authenticationService.UpdateCookie && authenticationService.CookieData != null)
            {
                ctx.Response.Cookies.Append(
                    "AuthenticationToken",
                    authenticationService.CookieData,
                    new CookieOptions
                    {
                        SameSite = hostEnvironment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
                        Secure = !hostEnvironment.IsDevelopment(),
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });
            }
            return Results.Ok(new BaseResponse() { Success = true });
        }).AllowAnonymous();

        return app;
    }
}
