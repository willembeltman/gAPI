using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace gAPI.AutoComparer;

public static class ComparerExtentions
{
    public static void AddCustomComparers(this IServiceCollection serviceCollection, params Assembly[] assembliesToScan)
    {
        // Als geen assemblies opgegeven zijn, neem de entry assembly
        if (assembliesToScan == null || assembliesToScan.Length == 0)
        {
            assembliesToScan = new[] { Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly() };
        }

        var customCompares = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(a =>
                a.BaseType != null &&
                a.BaseType.IsGenericType &&
                a.BaseType.GetGenericTypeDefinition() == typeof(CustomCompare<,>))
            .ToArray();

        foreach (var mapping in customCompares)
        {
            serviceCollection.AddScoped(mapping.BaseType!, mapping);
        }
    }
    public static void AttachComparer(this IApplicationBuilder appliationBuilder)
    {
        Comparer.SetServiceProvider(appliationBuilder.ApplicationServices);
    }
}
