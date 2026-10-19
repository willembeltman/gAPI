using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusRoutingDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusRoutingDto value, BusRoutingDto otherValue)
    {
        if (value.FromBusConnectionId != otherValue.FromBusConnectionId) return true;
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.MethodId != otherValue.MethodId) return true;
        if (value.TimeOut != otherValue.TimeOut) return true;
        if (value.deliveryMode != otherValue.deliveryMode) return true;
        if (value.To != otherValue.To) return true;
        if (value.ToBusConnectionId != otherValue.ToBusConnectionId) return true;
        return false;
    }
}