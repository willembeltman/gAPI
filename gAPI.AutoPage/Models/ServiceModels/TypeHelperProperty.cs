using gAPI.AutoPage.Interfaces;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class TypeHelperProperty : ITypeHelperProperty
{
    public TypeHelperProperty(ServiceContext serviceContext, TypeHelper parentTypeHelper, IPropertySymbol propertySymbol, ITypeSymbol[] history)
    {
        ServiceContext = serviceContext;
        ParentType = parentTypeHelper;
        PropertySymbol = propertySymbol;
        History = history;

        Name = propertySymbol.Name;
        Title = Name;
        var TitleAttribute = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TitleAttribute");
        if (TitleAttribute != null)
        {
            Title = TitleAttribute.ConstructorArguments[0].Value?.ToString() ?? Title;
        }

        IsNullable = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
        IsReadOnly = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsReadOnlyAttribute");
        IsPassword = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsPasswordAttribute");
        IsStateManaged = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStateManagedAttribute");
        IsImmutable = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsImmutableAttribute");
        IsUnique = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUniqueAttribute");
        IsKey = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "KeyAttribute");
        IsName = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsNameAttribute");
        IsStorageFileUrlProperty = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStorageFileUrlPropertyAttribute");

        var isForeignNameAttr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsForeignNameAttribute");

        if (isForeignNameAttr != null)
        {
            var arg = isForeignNameAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string strValue)
            {
                IsForeignName = true;
                IsForeignNameString = strValue;
            }
        }

        var isForeignKeyAttr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsForeignKeyAttribute");
        if (isForeignKeyAttr != null)
        {
            var arg = isForeignKeyAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Type &&
                arg.Value is ITypeSymbol typeSymbol)
            {
                IsForeignKey = true;
                IsForeignKeyType = new TypeHelper(serviceContext, typeSymbol);
            }
        }

        PropertyTypeSymbol = propertySymbol.Type;
        Type = new TypeHelper(serviceContext, PropertyTypeSymbol, IsNullable, history);
    }

    public IPropertySymbol PropertySymbol { get; }
    public ITypeSymbol[] History { get; }
    public ITypeSymbol PropertyTypeSymbol { get; }
    public ServiceContext ServiceContext { get; }
    public TypeHelper ParentType { get; }
    public TypeHelper Type { get; }
    public TypeHelper? IsForeignKeyType { get; }
    public string Name { get; }
    public string Title { get; }
    public bool IsNullable { get; }
    public bool IsReadOnly { get; }
    public bool IsStateManaged { get; }
    public bool IsImmutable { get; }
    public bool IsUnique { get; }
    public bool IsKey { get; }
    public bool IsName { get; }
    public bool IsStorageFileUrlProperty { get; }
    public bool IsForeignName { get; }
    public string? IsForeignNameString { get; }
    public bool IsPassword { get; }
    public bool IsForeignKey { get; }

    TypeDigger? TypeDiggerInner { get; set; }
    public TypeDigger TypeDigger => TypeDiggerInner ??= new TypeDigger(ServiceContext, PropertyTypeSymbol, IsNullable);

    TypeHelperPropertyAttribute[]? AttributesInner { get; set; }
    public TypeHelperPropertyAttribute[] GetAttributes() => AttributesInner ??= PropertySymbol.GetAttributes()
        .Where(a => a.AttributeClass != null)
        .Select(attr => new TypeHelperPropertyAttribute(this, attr, attr.AttributeClass!, History))
        .ToArray();

    ITypeHelper ITypeHelperProperty.Type => Type;
    ITypeHelper? ITypeHelperProperty.IsForeignKeyType => IsForeignKeyType;
    ITypeHelperPropertyAttribute[] ITypeHelperProperty.GetAttributes() => GetAttributes();
}
