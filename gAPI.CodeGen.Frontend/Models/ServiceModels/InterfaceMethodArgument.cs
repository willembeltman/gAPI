using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Helpers;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels;

public class InterfaceMethodArgument
{
    public InterfaceMethodArgument(ServiceContext dataModel, InterfaceMethod serviceMethod, ParameterInfo parameterInfo)
    {
        DataModel = dataModel;
        ServiceMethod = serviceMethod;
        ParameterInfo = parameterInfo;

        Name = parameterInfo.Name;

        var context = new NullabilityInfoContext();
        var nullabilityInfo = context.Create(parameterInfo);
        IsNullable = nullabilityInfo.ReadState == NullabilityState.Nullable;

        //IsIFormFile = ParameterInfo.Type.Name == "IFormFile";
        IsIFormFile = parameterInfo.ParameterType.Name == "IFormFile";
        //IsValueType = parameterInfo.Type.IsValueType;
        IsValueType = parameterInfo.ParameterType.IsValueType;
    }

    public ServiceContext DataModel { get; }
    public InterfaceMethod ServiceMethod { get; }
    public ParameterInfo ParameterInfo { get; }
    public string? Name { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }

    TypeHelper? _ParameterType { get; set; }
    public TypeHelper ParameterType
    {
        get
        {
            _ParameterType = _ParameterType ?? new TypeHelper(ParameterInfo.ParameterType, IsNullable);
            return _ParameterType;
        }
    }

    TypeDigger? _ParameterTypeRapport { get; set; }
    public TypeDigger ParameterTypeRapport
    {
        get
        {
            _ParameterTypeRapport = _ParameterTypeRapport ?? new TypeDigger(DataModel, ParameterType.Type, IsNullable);
            return _ParameterTypeRapport;
        }
    }
}