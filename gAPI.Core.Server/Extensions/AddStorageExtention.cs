using gAPI.Core.Server.Config;
using gAPI.Core.Server.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Core.Server.Extensions;

public static class AddStorageExtension
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfigurationManager configuration,
        TimeProvider? dateTime = null)
    {
        var config = configuration.CreateServerConfig();
        return AddStorage(
            services,
            config.StorageConnectionString ?? throw new Exception("Connectionstring cannot be empty."),
            dateTime);
    }

    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        ServerConfig serverConfig)
    {
        if (serverConfig.StorageConnectionString != null)
            services.AddStorage(serverConfig.StorageConnectionString);
        return services;
    }

    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        string storageConnectionString,
        TimeProvider? dateTime = null)
    {
        dateTime ??= TimeProvider.System;
        var storageService = new StorageService(storageConnectionString, dateTime);
        services.AddSingleton<StorageService>(sp => storageService);
        services.AddSingleton<IStorageService>(sp => storageService);
        return services;
    }
}
