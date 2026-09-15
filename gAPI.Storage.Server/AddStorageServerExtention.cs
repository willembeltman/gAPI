using gAPI.Storage.Server.Config;
using gAPI.Storage.Server.Data;
using gAPI.Storage.Server.Helpers;
using gAPI.Storage.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace gAPI.Storage.Server;

public static class AddStorageServerExtension
{
    public static WebApplicationBuilder AddStorageServer(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        }
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.Configure<LocalStorageServerConfig>(
            builder.Configuration.GetSection("LocalStorageServerConfig")
        );

        var appConfig = builder.Configuration.GetSection("LocalStorageServerConfig").Get<LocalStorageServerConfig>()!;

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                //options.RequireHttpsMetadata = false; // for local dev
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(appConfig.SuperSecretKeyArray)
                };
            });

        builder.Services.AddScoped<LocalStorageService>();
        builder.Services.AddSingleton<ApplicationDbContext>();
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        StartupHelper.ShowStorageStarted(appConfig.Credentials.UserName);

        return builder;
    }


    public static WebApplication MapStorageServer(this WebApplication app)
    {
        app.MapControllers();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();

        return app;
    }
}
