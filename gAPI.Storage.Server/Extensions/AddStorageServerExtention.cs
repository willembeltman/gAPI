using gAPI.Storage.Server.Config;
using gAPI.Storage.Server.Data;
using gAPI.Storage.Server.Helpers;
using gAPI.Storage.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Storage.Server.Extensions;

public static class AddStorageServerExtension
{
    public static IServiceCollection AddStorageServer(
        this IServiceCollection services,
        IConfigurationManager configuration)
    {
        var appConfig = configuration.CreateLocalStorageServerConfig();
        return AddStorageServer(services, appConfig);
    }

    private static IServiceCollection AddStorageServer(
        this IServiceCollection services, 
        LocalStorageServerConfig appConfig)
    {
        services.AddSingleton(appConfig);
        services.AddScoped<StorageServiceBus>();
        services.AddSingleton<ApplicationDbContext>();
        services.AddAuthorization();
        services.AddControllers();

        StartupHelper.ShowStorageStarted(appConfig.BaseUrl);

        return services;
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
