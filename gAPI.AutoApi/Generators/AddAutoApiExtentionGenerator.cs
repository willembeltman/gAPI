
namespace gAPI.AutoApi.Generators;

internal class AddAutoApiExtentionGenerator : BaseGenerator
{
    internal AddAutoApiExtentionGenerator(ServiceContext serviceContext)
    {
        ServiceContext = serviceContext;

        Directory = serviceContext.Config.AddAutoApiServices_Destination.Directory;
        Namespace = serviceContext.Config.AddAutoApiServices_Destination.Namespace;

        Name = "AutoApiExtention";
        FileName = $"{Name}.g.cs";
    }

    public ServiceContext ServiceContext { get; }

    internal void GenerateCode()
    {
        Reg("Microsoft.AspNetCore.Authentication");
        Reg("Microsoft.AspNetCore.HttpOverrides");
        Reg("System.Reflection");
        Reg("Microsoft.AspNetCore.Builder");

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoApi<TInterface, TImplementation>(this IServiceCollection services, string frontendUrl, params Assembly[] assembliesToScan)
        where TInterface : class, gAPI.Interfaces.IServerAuthenticationService
        where TImplementation : class, gAPI.Interfaces.IServerAuthenticationService, TInterface
    {{
        // JSON standaard op invariant zetten
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {{
            options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.SerializerOptions.PropertyNamingPolicy = null;
            options.SerializerOptions.WriteIndented = false;
        }});

        // Add normal asp.net core controllers
        services.AddControllers();

        // Http context accessor for gAPI
        services.AddHttpContextAccessor();

        // Remaining services
        services.AddAutoApiServices();

        // Add Cors
        services.AddCors(options =>
        {{
            options.AddPolicy(""AllowSpecificOrigin"", policy =>
            {{
                policy.WithOrigins(frontendUrl)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }});
        }});

        // Add gAPI server authentication 
        services.AddScoped<TImplementation>() ;
        services.AddScoped<TInterface>(sp => sp.GetRequiredService<TImplementation>())   ;
        services.AddScoped<gAPI.Interfaces.IServerAuthenticationService>(sp => sp.GetRequiredService<TImplementation>());
        services.AddAuthentication(""gAPI"")
                        .AddScheme<AuthenticationSchemeOptions, BSD.Core.Authentication.ServerAuthenticationHandler>(""gAPI"", _ => {{ }});
        services.AddAuthorization();
        services.AddScoped<BSD.Core.Authentication.IServerAuthenticationSecurity, BSD.Core.Authentication.ServerAuthenticationSecurity>();
        services.AddScoped<BSD.Core.Authentication.ServerAuthenticationStateFactory>();
        services.AddScoped<BSD.Core.Authentication.ServerAuthenticationStateMapping>();

    }}

    public static void MapAutoApi(this WebApplication app)
    {{
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {{
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        }});

        app.UseCors(""AllowSpecificOrigin"");

        app.MapControllers();

        app.UseMiddleware<BSD.Core.Authentication.ServerAuthenticationMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
    }}
}}
";

    }
}