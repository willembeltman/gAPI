using gAPI.Core.Client.Config;
using gAPI.Generated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Storage.LanCloud.Host;

public static class AddLanCloudHostExtension
{
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
        services.AddAutoWssClient(config);
        services.AddAutoAuthClient(config);

        services.AddHostedService<HostHub>();
        services.AddSingleton(config);
        services.AddSingleton<ClientConfig>(sp => sp.GetRequiredService<LanCloudHostConfig>());

        return services;
    }
}