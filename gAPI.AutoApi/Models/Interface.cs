using gAPI.AutoApi.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoApi.Models;

internal class Interface
{
    public Interface(ServiceContext dataModel, INamedTypeSymbol namedTypeSymbol, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        NamedTypeSymbol = namedTypeSymbol;

        Name = NamedTypeSymbol.Name;
        FullName = NamedTypeSymbol.ToDisplayString();
        Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

        Title = Name;
        Title = ServiceNameHelper.RemoveInterfacePrefix(Title);
        var generateApiAttr = NamedTypeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "GenerateApiAttribute");
        if (generateApiAttr != null)
        {
            Title = generateApiAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? Title;
        }
        Title = ServiceNameHelper.RemoveServiceName(Title);

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
            .SingleOrDefault();
    }

    public INamedTypeSymbol NamedTypeSymbol { get; }
    public string Name { get; }
    public string FullName { get; }
    public string Namespace { get; }
    public string Title { get; }
    public bool IsAuthorized { get; }
    public bool IsHidden { get; }
    public InterfaceMethod[] Methods { get; }
    public Service Service { get; }
}