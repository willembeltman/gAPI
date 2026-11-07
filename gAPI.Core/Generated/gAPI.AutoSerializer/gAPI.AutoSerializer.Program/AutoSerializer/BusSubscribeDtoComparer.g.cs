using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusSubscribeDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusSubscribeDto value, BusSubscribeDto otherValue)
    {
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.BusConnectionId != otherValue.BusConnectionId) return true;
        return false;
    }
}