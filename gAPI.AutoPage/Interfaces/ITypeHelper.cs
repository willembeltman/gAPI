using System.Collections.Generic;

namespace gAPI.AutoPage.Interfaces;

public interface ITypeHelper : ISharedReference
{
    bool IsNullable { get; }
    ITypeHelper[] UnderlayingTypes { get; }
    bool IsBaseResponse { get; }
    bool IsBaseResponseT { get; }
    bool IsBaseListResponseT { get; }
    bool IsTask { get; }
    bool IsVoid { get; }
    bool IsGuid { get; }
    bool IsString { get; }
    bool IsNumber { get; }
    bool IsCheckbox { get; }
    bool IsEnum { get; }
    bool IsDateTime { get; }
    bool IsPrimitive { get; }
    bool IsValueType { get; }

    ITypeHelperProperty[] GetProperties();
}
