using gAPI.AutoApiClient.Configs;
using gAPI.AutoApiClient.Helpers;
using gAPI.AutoApiClient.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoApiClient.Contexts;

internal class ServiceContext
{
    internal ServiceContext(Compilation compilation, ClientConfig config)
    {
        Config = config ?? throw new Exception("ServerConfig cannot be null");

        var allSymbols = compilation.GlobalNamespace.GetAllTypes();
        var interfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Attributes.GenerateApiAttribute") &&
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

        //State = allSymbols
        //    .Where(t =>
        //        t.TypeKind == TypeKind.Class &&
        //        t.HasAttribute("gAPI.Attributes.IsStateDtoAttribute"))
        //    .Select(a => new SharedReference(a))
        //    .FirstOrDefault() ?? throw new InvalidOperationException(
        //        "The `State` dto is missing or does not have the required " +
        //        "`gAPI.Attributes.IsStateDtoAttribute`. " +
        //        "Ensure your shared project defines a `State` dto and that it is annotated with " +
        //        "`[IsStateDtoAttribute]`.");

    }

    public ClientConfig Config { get; }
    public Interface[] Interfaces { get; }
    public EnumDto[] Enums { get; }
    public Dto[] Dtos { get; }
    //public SharedReference State { get; }

}
