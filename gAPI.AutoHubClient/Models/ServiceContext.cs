using gAPI.AutoHubClient.Helpers;
using gAPI.AutoHubClient.Models.Configs;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoHubClient.Models;

internal class ServiceContext
{
    internal ServiceContext(Compilation compilation, ClientConfig config)
    {
        Config = config ?? throw new Exception("ClientConfig cannot be null");

        var allSymbols = compilation.GlobalNamespace.GetAllTypes();
        var interfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Attributes.GenerateHubAttribute") &&
                Config.BaseNamespaces.Any(a => t.ContainingNamespace.ToDisplayString().StartsWith(a)))
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

    public ClientConfig Config { get; }
    public Interface[] Interfaces { get; }
    public EnumDto[] Enums { get; }
    public Dto[] Dtos { get; }
    //public ClientHandler[] ClientHandlers { get; }
}
