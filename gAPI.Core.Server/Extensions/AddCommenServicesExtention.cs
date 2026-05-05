using gAPI.Core.Server.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Core.Server.Extensions;

public static class AddCommenServicesExtension
{
    public static IServiceCollection AddCommenServices(this IServiceCollection services, ServerConfig serverConfig)
    {
        services.AddSingleton(serverConfig);
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
