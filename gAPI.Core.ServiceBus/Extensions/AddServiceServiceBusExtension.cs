using gAPI.Core.ServiceBus.Interfaces;
using gAPI.Core.ServiceBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using System.Reflection;

namespace gAPI.Core.ServiceBus.Extensions;

public static class AddServiceServiceBusExtension
{
    public static IServiceCollection AddServiceBus(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitServiceBusConnectionProvider, RabbitServiceBusConnectionProvider>();
        services.AddSingleton<IServiceBusHandlerRegistry, ServiceBusHandlerRegistry>();
        services.AddSingleton<IServiceBusReceiver, ServiceBusReceiver>();
        services.AddSingleton<IServiceBusSender, ServiceBusSender>();
        services.AddSingleton<IConsoleService, ConsoleService>();

        // Registreer automatisch alle handlers
        services.RegisterAllHandlers();

        return services;
    }

    private static void RegisterAllHandlers(this IServiceCollection services)
    {
        if (DependencyContext.Default == null)
            return;

        // Haal alle libraries op die gekoppeld zijn aan de applicatie
        var assemblies = DependencyContext.Default.RuntimeLibraries
            .Select(library =>
            {
                try
                {
                    // Laad de assembly expliciet in het geheugen als dat nog niet zo is
                    return Assembly.Load(new AssemblyName(library.Name));
                }
                catch
                {
                    return null;
                }
            })
            .Where(a => a != null)
            .ToList();

        // Zoek nu in alle geladen assemblies naar de handlers
        var handlerTypes = assemblies
            .SelectMany(a =>
            {
                try { return a!.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
            })
            .Where(t => t != null && t.IsClass && !t.IsAbstract)
            .ToArray();

        var handlerFullName = typeof(IHandler).FullName;
        if (handlerFullName == null)
            throw new Exception("Wtf?");
        foreach (var item in handlerTypes)
        {
            if (item == null) continue;
            var inter = item.GetInterface(handlerFullName);
            if (inter == null) continue;
            services.AddTransient(item);
        }
    }
}
