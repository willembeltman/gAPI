using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusSendRequestCancelledDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusSendRequestCancelledDto value, BusSendRequestCancelledDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.Reason != otherValue.Reason) return true;
        return false;
    }
}