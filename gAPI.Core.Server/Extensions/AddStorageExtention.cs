using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Core.Server.Extensions;

public static class AddStorageExtension
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        ServerConfig serverConfig)
    {
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
