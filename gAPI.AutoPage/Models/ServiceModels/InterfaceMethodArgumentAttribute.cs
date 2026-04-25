using gAPI.AutoPage.Interfaces;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoPage.Models.ServiceModels;

public class InterfaceMethodArgumentAttribute(
    InterfaceMethodArgument parent,
    AttributeData attr, 
    INamedTypeSymbol named) // is attr.AttributeClass!
    : ITypeHelperPropertyAttribute
{
    TypeHelper? TypeInner { get; set; }
    public TypeHelper Type => TypeInner ??= new TypeHelper(parent.ServiceContext, named, false);

    ITypeHelper ITypeHelperPropertyAttribute.Type => Type;

    public string Name => named.Name;
    public string? Namespace => named.ContainingNamespace.ToDisplayString();
    public string? FullName => named.ToDisplayString();

    public override string ToString()
    {
        var syntax = attr.ApplicationSyntaxReference?.GetSyntax();
        return syntax?.ToFullString() ?? attr.ToString();
    }

    public string ToNameString()
    {
        return ToString().Substring(Namespace?.Length + 1 ?? 0);
    }
}