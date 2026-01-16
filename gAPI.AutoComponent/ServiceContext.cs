using gAPI.AutoComponent.Configs;
using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Models.ServiceModels;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoComponent;

public class ServiceContext
{
    public ServiceContext(Compilation compilation, ClientConfig config)
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

        foreach (var @interface in Interfaces.Where(a => a.Client == null))
        {
            throw new InvalidOperationException(
                $"No implementation of the service interface a`{@interface.FullName}` was found. " +
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

        Enums = collector.Enums
            .Select(namedTypeSymbol => new EnumDto(namedTypeSymbol))
            .ToArray();

        Dtos = collector.Dtos
            .Select(namedTypeSymbol => new Dto(this, namedTypeSymbol))
            .ToArray();

        ToFormFileAsyncExtention = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsToFormFileAsyncExtentionAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "Please add gAPI.AutoClient. ToFormFileAsyncExtention helper is missing.");

        FormFile = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsFormFileAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "Please add gAPI.AutoClient. FormFile is missing.");

        BaseResponse = new SharedReference("gAPI.Dtos", "BaseResponse");
        BaseResponseT = new SharedReference("gAPI.Dtos", "BaseResponseT");
        BaseListResponseT = new SharedReference("gAPI.Dtos", "BaseListResponseT");

        State = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsStateDtoAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "The `State` dto is missing or does not have the required " +
                "`gAPI.Attributes.IsStateDtoAttribute`. " +
                "Ensure your shared project defines a `State` dto and that it is annotated with " +
                "`[IsStateDtoAttribute]`.");
    }

    public ClientConfig Config { get; }
    public Interface[] Interfaces { get; }
    public EnumDto[] Enums { get; }
    public Dto[] Dtos { get; }
    public SharedReference ToFormFileAsyncExtention { get; }
    public SharedReference FormFile { get; }
    public SharedReference State { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
}
