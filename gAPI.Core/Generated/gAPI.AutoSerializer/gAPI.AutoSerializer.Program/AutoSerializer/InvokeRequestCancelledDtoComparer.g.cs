using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestCancelledDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeRequestCancelledDto value, InvokeRequestCancelledDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.Reason != otherValue.Reason) return true;
        return false;
    }
}