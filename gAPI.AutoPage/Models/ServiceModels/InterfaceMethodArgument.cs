using gAPI.AutoPage.Interfaces;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class InterfaceMethodArgument : ICrudMethodArgument
{
    public InterfaceMethodArgument(ServiceContext serviceContext, InterfaceMethod serviceMethod, IParameterSymbol parameterSymbol)
    {
        ServiceContext = serviceContext;
        InterfaceMethod = serviceMethod;
        ParameterSymbol = parameterSymbol;

        Name = parameterSymbol.Name;

        Title = Name;
        var TitleAttribute = parameterSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TitleAttribute");
        if (TitleAttribute != null)
        {
            Title = TitleAttribute.ConstructorArguments[0].Value?.ToString() ?? Title;
        }


        IsPassword = parameterSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsPasswordAttribute");
        IsNullable =
            parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        IsIFormFile = ParameterSymbol.Type.Name == "IFormFile";
        IsValueType = parameterSymbol.Type.IsValueType;


        var isForeignKeyAttr = parameterSymbol.GetAttributes()
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

        IsNullable = parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        IsReadOnly = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsReadOnlyAttribute");
        IsPassword = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsPasswordAttribute");
        IsStateManaged = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStateManagedAttribute");
        IsImmutable = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsImmutableAttribute");
        IsUnique = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUniqueAttribute");
        IsKey = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "KeyAttribute");
        IsName = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsNameAttribute");
        IsStorageFileUrlProperty = parameterSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStorageFileUrlPropertyAttribute");

    }

    public ServiceContext ServiceContext { get; }
    public InterfaceMethod InterfaceMethod { get; }
    public IParameterSymbol ParameterSymbol { get; }
    public string Name { get; }
    public string Title { get; }
    public bool IsPassword { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }
    public bool IsForeignKey { get; }
    public ITypeHelper? IsForeignKeyType { get; }
    public bool IsReadOnly { get; }
    public bool IsForeignName { get; }
    public bool IsStateManaged { get; }
    public bool IsImmutable { get; }
    public bool IsUnique { get; }
    public bool IsStorageFileUrlProperty { get; }
    public bool IsKey { get; }
    public bool IsName { get; }

    TypeDigger? TypeDiggerInner { get; set; }
    public TypeDigger TypeDigger => TypeDiggerInner ??= new TypeDigger(ServiceContext, ParameterSymbol.Type, IsNullable);

    InterfaceMethodArgumentAttribute[]? AttributesInner { get; set; }

    public InterfaceMethodArgumentAttribute[] GetAttributes() =>
        AttributesInner ??= ParameterSymbol
            .GetAttributes()
            .Where(a => a.AttributeClass != null)
            .Select(attr => new InterfaceMethodArgumentAttribute(this, attr, attr.AttributeClass!))
            .ToArray();


    public TypeHelper Type => TypeDigger.StartType;
    public TypeHelper TypeBase => TypeDigger.Type;
    ITypeHelper ITypeHelperProperty.Type => Type;
    ITypeHelperPropertyAttribute[] ITypeHelperProperty.GetAttributes() => GetAttributes();
}