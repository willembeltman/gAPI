using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Models.Configs;
using gAPI.AutoComponent.Models.ServiceModels;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoComponent.Models;

public class ServiceContext
{
    public ServiceContext(Compilation compilation, ComponentConfig config, INamedTypeSymbol[] allSymbols)
    {
        var interfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Attributes.GenerateApiAttribute") &&
                config.BaseNamespaces.Any(a => t.ContainingNamespace.ToDisplayString().StartsWith(a)))
            .ToArray();

        Interfaces = [.. interfaceSymbols.Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))];

        var interfacesWithoutService = Interfaces
            .Where(a => a.Client == null)
            .ToArray();

        if (interfacesWithoutService.Length > 0)
        {
            throw new InvalidOperationException(
                $"No implementation interfaces was found: {(string.Join(", ", interfacesWithoutService.Select(a => $"`{a.FullName}`")))}. " +
                "This usually means your project does not reference the assembly containing the generated client, " +
                "the client was not generated correctly, or no implementation exists " +
                "(why are you using gAPI.AutoComponents without gAPI.AutoClient? but anyways). " +
                "To fix this:\n" +
                "1. Ensure the project referencing gAPI.AutoClient is referenced by the project using gAPI.AutoComponents. " +
                "   You can also combine both projects if you prefer, but you can't use them in your Razor project directly.\n" +
                "2. Verify that a public client class implementing the interface exists.\n" +
                "3. Rebuild the solution to regenerate clients (sometimes 2–3 times)."
            );
        }

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

        Enums = [.. collector.Enums.Select(namedTypeSymbol => new EnumDto(namedTypeSymbol))];

        Dtos = [.. collector.Dtos.Select(namedTypeSymbol => new Dto(this, namedTypeSymbol))];
    }

    public Interface[] Interfaces { get; }
    public EnumDto[] Enums { get; }
    public Dto[] Dtos { get; }
}
