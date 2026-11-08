using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusUnsubscribeDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusUnsubscribeDto value, BusUnsubscribeDto otherValue)
    {
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.BusConnectionId != otherValue.BusConnectionId) return true;
        return false;
    }
}