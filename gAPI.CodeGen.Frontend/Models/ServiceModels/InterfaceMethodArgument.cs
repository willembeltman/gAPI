using gAPI.Core.Attributes;
using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Helpers;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels;

public class InterfaceMethodArgument : ICrudlMethodArgument
{
    public InterfaceMethodArgument(ServiceContext dataModel, InterfaceMethod serviceMethod, ParameterInfo parameterInfo)
    {
        DataModel = dataModel;
        ServiceMethod = serviceMethod;
        ParameterInfo = parameterInfo;

        Name = parameterInfo.Name!;
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


    ITypeHelper ITypeHelperProperty.Type => ParameterType;
    public ITypeHelper? IsForeignKeyType => throw new NotImplementedException();
    public bool IsForeignKey => throw new NotImplementedException();
    public bool IsReadOnly => throw new NotImplementedException();
    public bool IsForeignName => throw new NotImplementedException();
    public bool IsStateManaged => throw new NotImplementedException();
    public bool IsImmutable => throw new NotImplementedException();
    public bool IsStorageFileUrlProperty => throw new NotImplementedException();
    public bool IsKey => throw new NotImplementedException();
    public bool IsName => throw new NotImplementedException();
    public AutoComponent.Models.ServiceModels.ITypeHelperPropertyAttribute[] GetAttributes()
    {
        throw new NotImplementedException();
    }







    public ServiceContext DataModel { get; }
    public InterfaceMethod ServiceMethod { get; }
    public ParameterInfo ParameterInfo { get; }
    public string Name { get; }
    public string Title { get; }
    public bool IsPassword { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }

    TypeHelper? ParameterTypeInner { get; set; }
    public TypeHelper ParameterType => ParameterTypeInner ??= new TypeHelper(ParameterInfo.ParameterType, IsNullable);

    TypeDigger? ParameterTypeRapportInner { get; set; }
    public TypeDigger ParameterTypeRapport => ParameterTypeRapportInner ??= new TypeDigger(DataModel, ParameterType.Type, IsNullable);

}