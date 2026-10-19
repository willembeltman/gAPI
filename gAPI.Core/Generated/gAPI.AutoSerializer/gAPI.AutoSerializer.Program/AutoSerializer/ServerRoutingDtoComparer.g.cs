using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerRoutingDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this ServerRoutingDto value, ServerRoutingDto otherValue)
    {
        if (value.From_FabricManagerId != otherValue.From_FabricManagerId) return true;
        if (value.From_FabricConnectionId != otherValue.From_FabricConnectionId) return true;
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.MethodId != otherValue.MethodId) return true;
        if (value.FabricConnectionId != otherValue.FabricConnectionId) return true;
        if (value.ClientConnectionId != otherValue.ClientConnectionId) return true;
        return false;
    }
}