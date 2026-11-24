using gAPI.Core.Server.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Extensions;

public static class FabricLoggingExtensions
{
    public static ILoggingBuilder AddFabricLogger(
        this ILoggingBuilder builder)
    {
        builder.Services.TryAddSingleton<ServerLogQueue>();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, FabricLoggerProvider>());

        return builder;
    }
}