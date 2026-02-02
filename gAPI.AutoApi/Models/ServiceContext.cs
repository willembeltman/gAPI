using gAPI.AutoApi.Helpers;
using gAPI.AutoApi.Models.Configs;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoApi.Models;

internal class ServiceContext
{
    internal ServiceContext(Compilation compilation, ServerConfig config)
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

        var notImplementedList = Interfaces
            .Where(a => a.Service == null)
            .ToArray();
        if (notImplementedList.Length > 0)
        {
            throw new Exception(
                "Not all [GenerateApi] interfaces are implemented: " +
                string.Join(", ", notImplementedList.Select(a => a.FullName)));
        }


        Services = Interfaces
            .Select(a => a.Service)
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

        IServerAuthenticationService = new SharedReference("gAPI.Interfaces.IServerAuthenticationService");
    }

    public ServerConfig Config { get; }
    public Interface[] Interfaces { get; }
    public EnumDto[] Enums { get; }
    public Dto[] Dtos { get; }
    public Service[] Services { get; }
    public SharedReference IServerAuthenticationService { get; internal set; }
}
