using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusInvokeRequestDoneDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusInvokeRequestDoneDto value, BusInvokeRequestDoneDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.Cancelled != otherValue.Cancelled) return true;
        if (value.ExceptionMessage != otherValue.ExceptionMessage) return true;
        return false;
    }
}