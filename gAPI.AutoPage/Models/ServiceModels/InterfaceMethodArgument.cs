using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class InterfaceMethodArgument : ICrudlMethodArgument
{
    public InterfaceMethodArgument(ServiceContext serviceContext, InterfaceMethod serviceMethod, IParameterSymbol parameterSymbol)
    {
        DataModel = serviceContext;
        ServiceMethod = serviceMethod;
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
    }

    public ServiceContext DataModel { get; }
    public InterfaceMethod ServiceMethod { get; }
    public IParameterSymbol ParameterSymbol { get; }
    public string Name { get; }
    public string Title { get; }
    public bool IsPassword { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }
    public bool IsForeignKey { get; }
    public TypeHelper? IsForeignKeyType { get; }
    TypeHelper? ParameterTypeInner { get; set; }
    public TypeHelper ParameterType => ParameterTypeInner ??= new TypeHelper(DataModel, ParameterSymbol.Type, IsNullable);

    TypeDigger? ParameterTypeRapportInner { get; set; }
    public TypeDigger ParameterTypeDigger => ParameterTypeRapportInner ??= new TypeDigger(DataModel, ParameterType.TypeSymbol, IsNullable);

    ITypeHelper ITypeHelperProperty.Type => ParameterType;

}