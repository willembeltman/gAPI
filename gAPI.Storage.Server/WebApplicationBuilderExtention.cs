using gAPI.Storage.Server.Config;
using gAPI.Storage.Server.Data;
using gAPI.Storage.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace gAPI.Storage.Server
{
    public static class WebApplicationBuilderExtention
    {
        public static void RegistrateServices(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddJsonFile("gapistoragesettings.json", optional: false, reloadOnChange: true);
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
                builder.Configuration.AddJsonFile("gapistoragesettings.Development.json", optional: true, reloadOnChange: true);
            }

            builder.Services.Configure<LocalStorageServerConfig>(
                builder.Configuration.GetSection("LocalStorageServerConfig")
            );

            var appConfig = builder.Configuration.GetSection("LocalStorageServerConfig").Get<LocalStorageServerConfig>()!;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(appConfig.SuperSecretKeyArray)
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddScoped<LocalStorageService>();
            builder.Services.AddSingleton<ApplicationDbContext>();

            builder.Services.AddControllers();
        }
    }
}
