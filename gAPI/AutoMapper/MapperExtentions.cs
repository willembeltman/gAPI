using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace gAPI.AutoMapper
{
    public static class MapperExtentions
    {
        public static void AddCustomMappings(this IServiceCollection serviceCollection, params Assembly[] assembliesToScan)
        {
            if (assembliesToScan == null || assembliesToScan.Length == 0)
            {
                assembliesToScan = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .ToArray();
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
