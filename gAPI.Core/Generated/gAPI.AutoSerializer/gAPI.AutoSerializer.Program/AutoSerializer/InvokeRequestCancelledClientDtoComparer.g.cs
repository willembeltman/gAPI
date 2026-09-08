using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestCancelledClientDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeRequestCancelledClientDto value, InvokeRequestCancelledClientDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.Reason != otherValue.Reason) return true;
        if (value.StateIsChanged != otherValue.StateIsChanged) return true;
        if (value.StateData != otherValue.StateData) return true;
        return false;
    }
}