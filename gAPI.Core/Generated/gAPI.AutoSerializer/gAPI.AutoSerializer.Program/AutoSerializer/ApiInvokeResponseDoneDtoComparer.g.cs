using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ApiInvokeResponseDoneDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this ApiInvokeResponseDoneDto value, ApiInvokeResponseDoneDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.MethodId != otherValue.MethodId) return true;
        if (value.SessionData != otherValue.SessionData) return true;
        if (value.StateData != otherValue.StateData) return true;
        return false;
    }
}