using gAPI.Attributes;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.Configs;
using gAPI.CodeGen.Frontend.Models.ServiceModels;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models;

public class ServiceContext
{
    public ServiceContext(FrontendConfig config)
    {
        var allTypes =
            config.Assemblies.SelectMany(a => a.GetTypes()).ToArray();

        var interfaceTypes = allTypes
            .Where(t =>
                t.IsInterface &&
                t.GetCustomAttributes(typeof(GenerateApiAttribute), inherit: true).Length > 0
            )
            .ToArray();

        Interfaces = interfaceTypes
            .Select(interfaceType => new Interface(this, interfaceType, allTypes))
            .ToArray();

        var collector = new TypeCollector(config);
        foreach (var interfaceType in interfaceTypes)
        {
            // Alleen publieke methods ophalen
            var methods = interfaceType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance
            );

            foreach (var method in methods)
            {
                // Parameters toevoegen
                foreach (var param in method.GetParameters())
                {
                    collector.Add(param.ParameterType);
                }

                // Returntype toevoegen
                collector.Add(method.ReturnType);
            }
        }

        Enums = collector.Enums
            .Select(namedTypeSymbol => new EnumDto(namedTypeSymbol))
            .ToArray();
        Dtos = collector.Dtos
            .Select(namedTypeSymbol => new Dto(this, namedTypeSymbol))
            .ToArray();

    }

    public Interface[] Interfaces { get; }
    public EnumDto[] Enums { get; }
    public Dto[] Dtos { get; }
}
