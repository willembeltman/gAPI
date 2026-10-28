using gAPI.Storage.Server.Data;
using gAPI.Storage.Server.Helpers;
using gAPI.Storage.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace gAPI.Storage.Server.Extensions;

public static class AddStorageServerExtension
{
    public static WebApplicationBuilder AddStorageServer(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        }
        builder.Configuration.AddEnvironmentVariables();

        var appConfig = builder.Configuration.CreateLocalStorageServerConfig();
        builder.Services.AddSingleton(appConfig);
        builder.Services.AddScoped<StorageServiceBus>();
        builder.Services.AddSingleton<ApplicationDbContext>();
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        StartupHelper.ShowStorageStarted(appConfig.BaseUrl);

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
