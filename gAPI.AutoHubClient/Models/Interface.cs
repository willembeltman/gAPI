using gAPI.AutoHubClient.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoHubClient.Models;

internal class Interface
{
    public Interface(ServiceContext dataModel, INamedTypeSymbol namedTypeSymbol, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        NamedTypeSymbol = namedTypeSymbol;

        Name = NamedTypeSymbol.Name;
        FullName = NamedTypeSymbol.ToDisplayString();
        Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

        ApiName = Name;
        ApiName = ServiceNameHelper.RemoveInterfacePrefix(ApiName);
        //ApiName = ServiceNameHelper.RemoveClientHandlerName(ApiName);

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

        //ClientHandler = allSymbols
        //    .Where(a =>
        //        a.TypeKind == TypeKind.Class &&
        //        a.Interfaces.Any(@interface => @interface.ToDisplayString() == namedTypeSymbol.ToDisplayString()))
        //    .Select(a => new ClientHandler(this, a))
        //    .SingleOrDefault();
    }

    public INamedTypeSymbol NamedTypeSymbol { get; }
    public string Name { get; }
    public string FullName { get; }
    public string Namespace { get; }
    public string ApiName { get; }
    public bool IsAuthorized { get; }
    public bool IsHidden { get; }
    public InterfaceMethod[] Methods { get; }
    //public ClientHandler ClientHandler { get; }
}