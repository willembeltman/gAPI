using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusInvokeRequestCancelledDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusInvokeRequestCancelledDto value, BusInvokeRequestCancelledDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.Reason != otherValue.Reason) return true;
        return false;
    }
}