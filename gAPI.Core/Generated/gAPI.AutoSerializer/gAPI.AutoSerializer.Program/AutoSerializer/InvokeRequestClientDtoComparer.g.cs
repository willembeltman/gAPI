using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestClientDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeRequestClientDto value, InvokeRequestClientDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        if (value.StateIsChanged != otherValue.StateIsChanged) return true;
        if (value.StateData != otherValue.StateData) return true;
        return false;
    }
}