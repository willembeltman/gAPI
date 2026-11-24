using gAPI.Core.Client.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Client.Extensions;

public static class WssLoggingExtensions
{
    public static ILoggingBuilder AddWssLogger(
        this ILoggingBuilder builder)
    {
        builder.Services.TryAddSingleton<ClientLogQueue>();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, WssLoggerProvider>());

        return builder;
    }
}