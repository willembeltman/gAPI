using gAPI.AutoSseClient.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoSseClient.Models;

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

    public override string ToString()
    {
        return Name;
    }
}