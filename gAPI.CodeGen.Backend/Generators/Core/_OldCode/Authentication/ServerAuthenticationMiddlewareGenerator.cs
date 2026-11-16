//namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

//public class ServerAuthenticationMiddlewareGenerator : BaseGenerator
//{
//    public ServerAuthenticationMiddlewareGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Core_AuthenticationDirectory;
//        Namespace = context.Config.Core_AuthenticationNamespace;

//        Context = context;

//        Name = "ServerAuthenticationMiddleware";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public IServerAuthenticationServiceGenerator IServerAuthenticationService => Context.IServerAuthenticationService;

//    public void GenerateCode()
//    {
//        Reg("Microsoft.AspNetCore.Http");
//        Reg("Microsoft.Extensions.Hosting");
//        Reg("System.Net");
//        Reg(IServerAuthenticationService);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//public class {Name}
//{{
//    private readonly RequestDelegate _next;

//    public {Name}(RequestDelegate next) => _next = next;

//    public async Task Invoke(
//        HttpContext ___httpContext,
//        {IServerAuthenticationService} authentication,
//        IHostEnvironment hostEnvironment)
//    {{
//        IPAddress? ___forwardedIp = ___httpContext.Connection.RemoteIpAddress;
//        if (___httpContext.Request.Headers.TryGetValue(""X-Forwarded-For"", out var ___ipHeader))
//        {{
//            var ___firstIp = ___ipHeader.ToString().Split(',')[0].Trim();
//            if (IPAddress.TryParse(___firstIp, out var ___parsedIp))
//                ___forwardedIp = ___parsedIp;
//        }}

//        var ___path = ___httpContext.Request.Path;
//        var ___queryString = ___httpContext.Request.QueryString;
//        if (___httpContext.Request.Headers.TryGetValue(""X-Forwarded-Uri"", out var ___uriHeader))
//        {{
//            if (Uri.TryCreate(new Uri(""http://dummy""), ___uriHeader.ToString(), out var ___uri))
//            {{
//                ___path = ___uri.AbsolutePath;
//                ___queryString = new QueryString(___uri.Query);
//            }}
//        }}

//        var initResult = await authentication.InitializeAsync(
//            ___path
//            ___queryString,
//            ___forwardedIp,
//            ___httpContext.Request.Cookies[""AuthenticationToken""],
//            ___httpContext.Request.Headers[""X-SessionId""],
//            ___httpContext.Request.Headers[""X-StateData""],
//            ___httpContext.RequestAborted);

//        if (initResult.Forbidden)
//        {{
//            ___httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
//            var response = hostEnvironment.IsDevelopment()
//                ? initResult.ForbiddenReason ?? ""Forbidden""
//                : ""Forbidden"";
//            await ___httpContext.Response.WriteAsync(response, ___httpContext.RequestAborted);
//            return;
//        }}

//        ___httpContext.Response.OnStarting(async () =>
//        {{
//            if (authentication.Initialized == false)
//                return;

//            var sessionId = authentication.SessionId;
//            var stateData = await authentication.GetStateDataAsync(___httpContext.RequestAborted);

//            ___httpContext.Response.Headers[""X-SessionId""] = sessionId;
//            ___httpContext.Response.Headers[""X-StateData""] = stateData;

//            if (!authentication.UpdateCookie)
//                return;

//            if (___httpContext.Request.Cookies.TryGetValue(""AuthenticationToken"", out var _))
//                ___httpContext.Response.Cookies.Delete(""AuthenticationToken"");

//            if (authentication.CookieData == null)
//                return;

//            ___httpContext.Response.Cookies.Append(
//                ""AuthenticationToken"",
//                authentication.CookieData,
//                new CookieOptions
//                {{
//                    SameSite = hostEnvironment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
//                    Secure = !hostEnvironment.IsDevelopment(),
//                    Expires = DateTimeOffset.UtcNow.AddDays(7)
//                }});
//        }});

//        await _next(___httpContext);
//    }}
//}}";
//        Save(false);
//    }
//}