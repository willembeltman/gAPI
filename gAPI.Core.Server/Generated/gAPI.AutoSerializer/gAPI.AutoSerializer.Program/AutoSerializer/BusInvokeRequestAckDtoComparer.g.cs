using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusInvokeRequestAckDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusInvokeRequestAckDto value, BusInvokeRequestAckDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        return false;
    }
}