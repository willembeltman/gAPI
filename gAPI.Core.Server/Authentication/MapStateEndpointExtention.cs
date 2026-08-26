using gAPI.Core.Dtos;
using gAPI.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace gAPI.Core.Server.Authentication;

public static class MapStateEndpointExtention
{
    public static WebApplication MapStateEndpoint(this WebApplication app)
    {
        app.MapGet("/__state", async (
            CancellationToken ct,
            HttpContext ctx,
            [FromHeader(Name = "X-SessionId")] string? sessionId,
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
                sessionId != null ? new StringValues(sessionId) : StringValues.Empty,
                stateData != null ? new StringValues(stateData) : StringValues.Empty,
                ct);
            if (initializeResult.Forbidden == true) return Results.Forbid();

            var initializedStateData = authenticationService.GetStateData();
            var cookieData = authenticationService.GetCookieData();

            ctx.Response.Headers["X-SessionId"] = authenticationService.SessionId.ToStringValues();
            ctx.Response.Headers["X-StateData"] = initializedStateData;
            if (authenticationService.IsCookieDataChanged() && cookieData != null)
            {
                ctx.Response.Cookies.Append(
                    "AuthenticationToken",
                    cookieData,
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
