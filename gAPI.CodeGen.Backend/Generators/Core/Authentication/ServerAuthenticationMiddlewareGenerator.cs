namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

public class ServerAuthenticationMiddlewareGenerator : BaseGenerator
{
    public ServerAuthenticationMiddlewareGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Core_AuthenticationDirectory;
        Namespace = context.Config.Core_AuthenticationNamespace;

        Context = context;

        Name = "ServerAuthenticationMiddleware";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public IServerAuthenticationServiceGenerator IServerAuthenticationService => Context.IServerAuthenticationService;

    public void GenerateCode()
    {
        Reg("Microsoft.AspNetCore.Http");
        Reg("Microsoft.Extensions.Hosting");
        Reg(IServerAuthenticationService);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}
{{
    private readonly RequestDelegate _next;

    public {Name}(RequestDelegate next) => _next = next;

    public async Task Invoke(
        HttpContext ctx,
        {IServerAuthenticationService} authentication,
        IHostEnvironment hostEnvironment)
    {{
        var initResult = await authentication.InitializeAsync(
            ctx.Request.Path,
            ctx.Request.QueryString,
            ctx.Connection.RemoteIpAddress,
            ctx.Request.Cookies[""AuthenticationToken""],
            ctx.Request.Headers[""X-SessionId""],
            ctx.Request.Headers[""X-StateData""],
            ctx.RequestAborted);

        if (initResult.Forbidden)
        {{
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            var response = hostEnvironment.IsDevelopment()
                ? initResult.ForbiddenReason ?? ""Forbidden""
                : ""Forbidden"";
            await ctx.Response.WriteAsync(response, ctx.RequestAborted);
            return;
        }}

        ctx.Response.OnStarting(async () =>
        {{
            if (authentication.Initialized == false)
                return;

            var sessionId = authentication.SessionId;
            var stateData = await authentication.GetStateData(ctx.RequestAborted);

            ctx.Response.Headers[""X-SessionId""] = sessionId;
            ctx.Response.Headers[""X-StateData""] = stateData;

            if (!authentication.UpdateCookie)
                return;

            if (ctx.Request.Cookies.TryGetValue(""AuthenticationToken"", out var _))
                ctx.Response.Cookies.Delete(""AuthenticationToken"");

            if (authentication.CookieData == null)
                return;

            ctx.Response.Cookies.Append(
                ""AuthenticationToken"",
                authentication.CookieData,
                new CookieOptions
                {{
                    SameSite = hostEnvironment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
                    Secure = !hostEnvironment.IsDevelopment(),
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                }});
        }});

        await _next(ctx);
    }}
}}";
        Save(false);
    }
}