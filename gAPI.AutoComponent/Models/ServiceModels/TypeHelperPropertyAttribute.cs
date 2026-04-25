using gAPI.AutoComponent.Interfaces;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoComponent.Models.ServiceModels;

public class TypeHelperPropertyAttribute(
    TypeHelperProperty parent, 
    AttributeData attr, 
    INamedTypeSymbol named,
    ITypeSymbol[] history)
    : ITypeHelperPropertyAttribute
{
    TypeHelper? TypeInner { get; set; }
    public TypeHelper Type => TypeInner ??= new TypeHelper(parent.ServiceContext, named, false, history);

    ITypeHelper ITypeHelperPropertyAttribute.Type => Type;

    public string Name => named.Name;
    public string? Namespace => named.ContainingNamespace.ToDisplayString();
    public string? FullName => named.ToDisplayString();

    public override string ToString()
    {
        return attr.ToString();
    }
    public string ToNameString()
    {
        return ToString().Substring(Namespace?.Length + 1 ?? 0);
    }
}
