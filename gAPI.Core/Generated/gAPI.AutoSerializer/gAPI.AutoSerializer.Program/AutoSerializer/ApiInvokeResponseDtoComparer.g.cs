using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ApiInvokeResponseDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this ApiInvokeResponseDto value, ApiInvokeResponseDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.MethodId != otherValue.MethodId) return true;
        if (value.SessionData != otherValue.SessionData) return true;
        if (value.StateData != otherValue.StateData) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        return false;
    }
}