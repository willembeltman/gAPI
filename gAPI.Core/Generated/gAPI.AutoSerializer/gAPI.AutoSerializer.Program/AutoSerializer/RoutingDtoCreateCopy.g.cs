using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class RoutingDtoCreateCopy
{
    [IsCreateCopy]
    public static RoutingDto CreateCopy(this RoutingDto value)
    {
        return new RoutingDto(value.From_FabricManagerId, value.From_FabricConnectionId, value.RequestId, value.ServiceId, value.MethodId, value.Timeout, value.UserId, value.SessionId);
    }
}