using gAPI.AutoApiClient.Helpers;
using gAPI.AutoApiClient.Models.Configs;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoApiClient.Models;

public class ServiceContext
{
    public ServiceContext(Compilation compilation, ClientConfig config, INamedTypeSymbol[] allSymbols)
    {
        var interfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Attributes.GenerateApiAttribute") &&
                config.BaseNamespaces.Any(a => t.ContainingNamespace.ToDisplayString().StartsWith(a)))
            .ToArray();

        Interfaces = interfaceSymbols
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
            .ToArray();

        var collector = new TypeCollector(config);
        foreach (var swi in interfaceSymbols)
        {
            foreach (var method in swi.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.DeclaredAccessibility != Accessibility.Public)
                    continue;

                foreach (var param in method.Parameters)
                {
                    collector.Add(param.Type);
                }

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
