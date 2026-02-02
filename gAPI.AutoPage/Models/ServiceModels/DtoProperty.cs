using gAPI.AutoPage.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class DtoProperty
{
    public DtoProperty(ServiceContext serviceContext, Dto dto, IPropertySymbol propertySymbol)
    {
        ServiceContext = serviceContext;
        Dto = dto;
        PropertySymbol = propertySymbol;

        Name = propertySymbol.Name;
        Title = Name;
        var TitleAttribute = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TitleAttribute");
        if (TitleAttribute != null)
        {
            Title = TitleAttribute.ConstructorArguments[0].Value?.ToString() ?? Title;
        }

        ResponseTypeSymbol = propertySymbol.Type;

        IsNullable = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
        IsReadOnly = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsReadOnlyAttribute");
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
    }

    public ServiceContext ServiceContext { get; }
    public Dto Dto { get; }
    public IPropertySymbol PropertySymbol { get; }

    public string Name { get; }
    public string Title { get; }
    public ITypeSymbol ResponseTypeSymbol { get; }
    public bool IsNullable { get; }
    public bool IsReadOnly { get; }
    public bool IsForeignName { get; }
    public string? IsForeignNameString { get; }
    public bool IsStateManaged { get; }
    public bool IsImmutable { get;  }
    public bool IsUnique { get; }
    public bool IsKey { get; }
    public bool IsName { get; }
    public bool IsStorageFileUrlProperty { get; }

    public string TypeSimpleName => PropertyType.FullName;

    TypeHelper? _PropertyType { get; set; }
    public TypeHelper PropertyType => _PropertyType = _PropertyType ?? new TypeHelper(ServiceContext, ResponseTypeSymbol, IsNullable);

    TypeDigger? _TypeDigger { get; set; }
    public TypeDigger TypeDigger => _TypeDigger = _TypeDigger ?? new TypeDigger(ServiceContext, ResponseTypeSymbol);

    public bool IsForeignKey { get; }
    public TypeHelper? IsForeignKeyType { get; }
}