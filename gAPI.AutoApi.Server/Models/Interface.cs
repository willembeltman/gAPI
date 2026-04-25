using gAPI.AutoApiServer.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace gAPI.AutoApiServer.Models;

public class Interface
{
    public Interface(ServiceContext dataModel, INamedTypeSymbol namedTypeSymbol, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        NamedTypeSymbol = namedTypeSymbol;

        Name = NamedTypeSymbol.Name;
        FullName = NamedTypeSymbol.ToDisplayString();
        Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

        CleanName = Name;
        CleanName = ServiceNameHelper.RemoveInterfacePrefix(CleanName);
        var GenerateHubAttribute = NamedTypeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "GenerateHubAttribute");
        if (GenerateHubAttribute != null)
        {
            CleanName = GenerateHubAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? CleanName;
        }
        CleanName = ServiceNameHelper.RemoveInterfacePrefix(CleanName).ToNameCase();

        Title = CleanName;
        var nameAttr = NamedTypeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TitleAttribute");
        if (nameAttr != null)
        {
            Title = nameAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? Title;
        }

        IsAuthorized = NamedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsAuthorizedAttribute");

        IsHidden = NamedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsHiddenAttribute");

        Methods = NamedTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Where(m => !m.GetAttributes().Any(attr => attr.AttributeClass?.Name == "IsHiddenAttribute"))
            .Select(methodSymbol => new InterfaceMethod(dataModel, this, methodSymbol))
            .ToArray();

        Service = allSymbols
            .Where(a =>
                a.TypeKind == TypeKind.Class &&
                a.Interfaces.Any(@interface => @interface.ToDisplayString() == namedTypeSymbol.ToDisplayString()))
            .Select(a => new Service(this, a))
            .FirstOrDefault();
    }

    public INamedTypeSymbol NamedTypeSymbol { get; }
    public string Name { get; }
    public string FullName { get; }
    public string Namespace { get; }
    public string CleanName { get; }
    public string Title { get; }
    public bool IsAuthorized { get; }
    public bool IsHidden { get; }
    public InterfaceMethod[] Methods { get; }
    public Service Service { get; }

    public override string ToString()
    {
        return Name;
    }
}