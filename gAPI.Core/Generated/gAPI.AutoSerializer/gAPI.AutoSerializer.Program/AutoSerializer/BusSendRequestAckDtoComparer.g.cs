using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusSendRequestAckDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusSendRequestAckDto value, BusSendRequestAckDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.Exception != otherValue.Exception) return true;
        return false;
    }
}