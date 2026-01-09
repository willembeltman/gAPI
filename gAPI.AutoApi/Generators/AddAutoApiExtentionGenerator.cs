
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
        Reg("gAPI.AutoMapper");
        Reg("Microsoft.AspNetCore.HttpOverrides");
        Reg("System.Reflection");
        Reg("Microsoft.AspNetCore.Builder");

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoApi(this IServiceCollection services, string frontendUrl, params Assembly[] assembliesToScan)
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

        // Add custom mappings from Business project
        //services.AddCustomMappings(assembliesToScan);

        // Add Cors
        services.AddCors(options =>
        {{
            options.AddPolicy(""AllowSpecificOrigin"", policy =>
            {{
                policy.WithOrigins(frontendUrl) // <-- jouw frontend
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }});
        }});
    }}

    public static void MapAutoApi(this WebApplication app)
    {{
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {{
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        }});

        //app.AttachMapper();
        app.UseCors(""AllowSpecificOrigin"");

        app.MapControllers();
    }}
}}
";

    }
}