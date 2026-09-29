using gAPI.Core.Client.Config;
using gAPI.Core.Client.Extensions;
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
        var clientConfig = configuration.CreateClientConfig();
        var hostConfig = configuration.CreateLanCloudHostConfig();
        return AddStorageLanCloudHost(services, clientConfig, hostConfig);
    }
    public static IServiceCollection AddStorageLanCloudHost(
        this IServiceCollection services,
        ClientConfig clientConfig,
        LanCloudHostConfig hostConfig)
    {
        services.AddSingleton(clientConfig);
        services.AddSingleton(hostConfig);

        services.AddAutoWssClient(clientConfig);
        services.AddAutoAuthClient(clientConfig);

        services.AddHostedService<HostHub>();

        return services;
    }
}