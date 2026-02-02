using gAPI.AutoApiClient.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoApiClient.Models;

public class Dto
{
    public Dto(ServiceContext dataModel, INamedTypeSymbol namedTypeSymbol)
    {
        NamedTypeSymbol = namedTypeSymbol;

        Name = NamedTypeSymbol.Name;
        FullName = NamedTypeSymbol.ToDisplayString();
        Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

        IsUser = NamedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUserAttribute");
        IsEntryPoint = NamedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsEntryPointAttribute");

        var isJunctionAttr = namedTypeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsFileDeleteAttribute");
        if (isJunctionAttr != null)
        {
            if (isJunctionAttr.ConstructorArguments.Length > 1)
            {
                IsJunction = true;
                if (isJunctionAttr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    isJunctionAttr.ConstructorArguments[0].Value is ITypeSymbol targetTypeSymbolLeft)
                {
                    JunctionLeftRealType = new TypeHelper(dataModel, targetTypeSymbolLeft);
                }
                if (isJunctionAttr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    isJunctionAttr.ConstructorArguments[0].Value is ITypeSymbol targetTypeSymbolRight)
                {
                    JunctionRightRealType = new TypeHelper(dataModel, targetTypeSymbolRight);
                }
            }
            else
            {
                // geef warning dat er niet genoeg constructor arguments zijn
                throw new Exception(
                    $"Junction table '{FullName}' requires two type arguments for left and right types.");
            }
        }

        Properties = NamedTypeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>
                !string.IsNullOrWhiteSpace(p.Name) &&
                !string.IsNullOrWhiteSpace(p.Type?.ToDisplayString()))
            .Select(propertySymbol => new DtoProperty(dataModel, this, propertySymbol))
            .ToArray();
    }

    public INamedTypeSymbol NamedTypeSymbol { get; }
    public string Name { get; }
    public string FullName { get; }
    public string Namespace { get; }
    public bool IsUser { get; }
    public bool IsEntryPoint { get; }
    public bool IsJunction { get; }
    public TypeHelper? JunctionLeftRealType { get; }
    public TypeHelper? JunctionRightRealType { get; }
    public DtoProperty[] Properties { get; }
}