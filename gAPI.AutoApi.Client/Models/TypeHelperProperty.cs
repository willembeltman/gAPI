using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoApiClient.Models;

public class TypeHelperProperty
{
    public TypeHelperProperty(TypeHelper parent, IPropertySymbol propertySymbol)
    {
        Parent = parent;

        Name = propertySymbol.Name;
        ResponseTypeSymbol = propertySymbol.Type;

        IsNullable = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
        IsReadOnly = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsReadOnlyAttribute");
        IsForeignName = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsForeignNameAttribute");
        IsStateManaged = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStateManagedAttribute");
        IsUnique = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUniqueAttribute");
        IsKey = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "KeyAttribute");
        IsStorageFileUrlProperty = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStorageFileUrlPropertyAttribute");
    }

    public TypeHelper Parent { get; }
    public ITypeSymbol ResponseTypeSymbol { get; }
    public string Name { get; }
    public bool IsNullable { get; }
    public bool IsReadOnly { get; }
    public bool IsForeignName { get; }
    public bool IsStateManaged { get; }
    public bool IsUnique { get; }
    public bool IsKey { get; }
    public bool IsStorageFileUrlProperty { get; }

    TypeHelper? PropertyTypeInner { get; set; }
    public TypeHelper PropertyType => PropertyTypeInner ??= new TypeHelper(Parent.ServiceContext, ResponseTypeSymbol, IsNullable);
}