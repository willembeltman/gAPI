using gAPI.Core.Client.Config;
using gAPI.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace gAPI.Storage.LanCloud.Host;

public static class AddLanCloudHostExtension
{
    public static IServiceCollection AddLanCloudHost(
        this HostApplicationBuilder builder)
    {
        return AddLanCloudHost(builder.Services, builder.Configuration);
    }
    public static IServiceCollection AddLanCloudHost(
        this IServiceCollection services,
        IConfigurationManager configuration)
    {
        var config = configuration.CreateLanCloudHostConfig();
        return AddStorageLanCloudHost(services, config);
    }
    public static IServiceCollection AddStorageLanCloudHost(
        this IServiceCollection services,
        LanCloudHostConfig config)
    {
        services.AddSingleton(config);

        services.AddAutoWssClient(config);
        services.AddAutoAuthClient(config);

        services.AddHostedService<HostHub>();
        services.AddSingleton<ClientConfig>(sp => sp.GetRequiredService<LanCloudHostConfig>());

        return services;
    }
}