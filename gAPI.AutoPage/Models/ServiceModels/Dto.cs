using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class Dto : IDto
{
    public Dto(ServiceContext serviceContext, INamedTypeSymbol namedTypeSymbol)
    {
        NamedTypeSymbol = namedTypeSymbol;
        Type = new TypeHelper(serviceContext, namedTypeSymbol);

        Name = NamedTypeSymbol.Name;
        Title = Name;
        var TitleAttribute = namedTypeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TitleAttribute");
        if (TitleAttribute != null)
        {
            Title = TitleAttribute.ConstructorArguments[0].Value?.ToString() ?? Title;
        }
        FullName = NamedTypeSymbol.ToDisplayString();
        Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

        IsUser = NamedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUserAttribute");
        IsAuthorized = NamedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsAuthorizedAttribute");
        IsEntryPoint = NamedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsEntryPointAttribute");
        IsICrudEntity = NamedTypeSymbol.AllInterfaces.Any(i => i.Name == "ICrudEntity");

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
                    JunctionLeftRealType = new TypeHelper(serviceContext, targetTypeSymbolLeft);
                }
                if (isJunctionAttr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    isJunctionAttr.ConstructorArguments[0].Value is ITypeSymbol targetTypeSymbolRight)
                {
                    JunctionRightRealType = new TypeHelper(serviceContext, targetTypeSymbolRight);
                }
            }
            else
            {
                // geef warning dat er niet genoeg constructor arguments zijn
                throw new Exception(
                    $"Junction table '{FullName}' requires two type arguments for left and right types.");
            }
        }

        Properties = [.. NamedTypeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>
                !string.IsNullOrWhiteSpace(p.Name) &&
                !string.IsNullOrWhiteSpace(p.Type?.ToDisplayString()))
            .Select(propertySymbol => new DtoProperty(serviceContext, this, propertySymbol))];
    }

    public INamedTypeSymbol NamedTypeSymbol { get; }
    public TypeHelper Type { get; }
    public string Name { get; }
    public string Title { get; }
    public string FullName { get; }
    public string Namespace { get; }
    public bool IsAuthorized { get; }
    public bool IsUser { get; }
    public bool IsEntryPoint { get; }
    public bool IsJunction { get; }
    public bool IsICrudEntity { get; }
    public TypeHelper? JunctionLeftRealType { get; }
    public TypeHelper? JunctionRightRealType { get; }
    public DtoProperty[] Properties { get; }

    ITypeHelper IDto.Type => Type;
}