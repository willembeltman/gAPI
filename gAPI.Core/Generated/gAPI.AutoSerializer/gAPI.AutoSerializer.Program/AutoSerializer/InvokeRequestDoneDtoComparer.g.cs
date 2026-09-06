using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestDoneDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeRequestDoneDto value, InvokeRequestDoneDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.StateIsChanged != otherValue.StateIsChanged) return true;
        if (value.StateData != otherValue.StateData) return true;
        return false;
    }
}