using gAPI.Storage.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Storage.Client.Extensions;

public static class AddStorageExtension
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfigurationManager configuration)
    {
        var config = configuration.CreateStorageConfig();
        return AddStorage(
            services,
            config);
    }

    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        StorageConfig config)
    {
        services.AddSingleton(config);
        services.AddSingleton<IStorageService, StorageService>();
        return services;
    }
}
