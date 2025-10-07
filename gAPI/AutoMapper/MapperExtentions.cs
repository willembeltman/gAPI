using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace gAPI.AutoMapper
{
    public static class MapperExtentions
    {
        public static void AddCustomMappings(this IServiceCollection serviceCollection, params Assembly[] assembliesToScan)
        {
            // Als geen assemblies opgegeven zijn, neem de entry assembly
            if (assembliesToScan == null || assembliesToScan.Length == 0)
            {
                assembliesToScan = new[] { Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly() };
            }

            var customMappings = assembliesToScan
                .SelectMany(a => a.GetTypes())
                .Where(a =>
                    a.BaseType != null &&
                    a.BaseType.IsGenericType &&
                    a.BaseType.GetGenericTypeDefinition() == typeof(CustomMapping<,>))
                .ToArray();

            foreach (var mapping in customMappings)
            {
                serviceCollection.AddScoped(mapping.BaseType, mapping);
            }
        }
        public static void AttachMapper(this IApplicationBuilder appliationBuilder)
        {
            Mapper.SetServiceProvider(appliationBuilder.ApplicationServices);
        }
    }
}
