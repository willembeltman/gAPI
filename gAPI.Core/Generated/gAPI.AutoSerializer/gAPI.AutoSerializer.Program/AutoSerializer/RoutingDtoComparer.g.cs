using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class RoutingDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this RoutingDto value, RoutingDto otherValue)
    {
        if (value.From_FabricManagerId != otherValue.From_FabricManagerId) return true;
        if (value.From_FabricConnectionId != otherValue.From_FabricConnectionId) return true;
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.MethodId != otherValue.MethodId) return true;
        if (value.Timeout != otherValue.Timeout) return true;
        if (value.UserId != otherValue.UserId) return true;
        if (value.SessionId != otherValue.SessionId) return true;
        return false;
    }
}