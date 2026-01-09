using gAPI.AutoSse.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoSse.Models;

internal class InterfaceMethodArgument
{
    internal InterfaceMethodArgument(ServiceContext dataModel, InterfaceMethod serviceMethod, IParameterSymbol parameterSymbol)
    {
        DataModel = dataModel;
        ServiceMethod = serviceMethod;
        ParameterSymbol = parameterSymbol;

        Name = parameterSymbol.Name;

        IsNullable =
            parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        IsIFormFile = ParameterSymbol.Type.Name == "IFormFile";
        IsValueType = parameterSymbol.Type.IsValueType;
    }

    public ServiceContext DataModel { get; }
    public InterfaceMethod ServiceMethod { get; }
    public IParameterSymbol ParameterSymbol { get; }
    public string Name { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }

    TypeHelper _ParameterType { get; set; }
    public TypeHelper ParameterType
    {
        get
        {
            _ParameterType = _ParameterType ?? new TypeHelper(DataModel, ParameterSymbol.Type, IsNullable);
            return _ParameterType;
        }
    }

    TypeDigger _ParameterTypeRapport { get; set; }
    public TypeDigger ParameterTypeRapport
    {
        get
        {
            _ParameterTypeRapport = _ParameterTypeRapport ?? new TypeDigger(DataModel, ParameterType.Type);
            return _ParameterTypeRapport;
        }
    }
}