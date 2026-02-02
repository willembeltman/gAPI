using gAPI.Attributes;
using gAPI.CodeGen.Frontend.Helpers;
using Microsoft.CodeAnalysis;
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
        Title = parameterInfo.GetCustomAttribute<TitleAttribute>()?.Name ?? Name;

        var context = new NullabilityInfoContext();
        var nullabilityInfo = context.Create(parameterInfo);
        IsPassword = parameterInfo.GetCustomAttribute<IsPasswordAttribute>() != null;
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
    public string? Title { get; }
    public bool IsPassword { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }

    TypeHelper? ParameterTypeInner { get; set; }
    public TypeHelper ParameterType => ParameterTypeInner ??= new TypeHelper(ParameterInfo.ParameterType, IsNullable);

    TypeDigger? ParameterTypeRapportInner { get; set; }
    public TypeDigger ParameterTypeRapport => ParameterTypeRapportInner ??= new TypeDigger(DataModel, ParameterType.Type, IsNullable);
}