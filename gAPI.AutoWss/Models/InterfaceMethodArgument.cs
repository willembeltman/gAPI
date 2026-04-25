using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoWssServer.Models;

public class InterfaceMethodArgument
{
    public InterfaceMethodArgument(ServiceContext dataModel, InterfaceMethod serviceMethod, IParameterSymbol parameterSymbol)
    {
        DataModel = dataModel;
        ServiceMethod = serviceMethod;
        ParameterSymbol = parameterSymbol;

        Name = parameterSymbol.Name;

        IsPassword = parameterSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsPasswordAttribute");
        IsNullable =
            parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        IsIFormFile = ParameterSymbol.Type.Name == "IFormFile";
        IsValueType = parameterSymbol.Type.IsValueType;
    }

    public ServiceContext DataModel { get; }
    public InterfaceMethod ServiceMethod { get; }
    public IParameterSymbol ParameterSymbol { get; }
    public string Name { get; }
    public bool IsPassword { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }

    TypeHelper? ParameterTypeInner { get; set; }
    public TypeHelper ParameterType => ParameterTypeInner ??= new TypeHelper(DataModel, ParameterSymbol.Type, IsNullable);

    public override string ToString()
    {
        return Name;
    }
}