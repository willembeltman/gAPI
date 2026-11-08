using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestAckDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeRequestAckDto value, InvokeRequestAckDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        return false;
    }
}