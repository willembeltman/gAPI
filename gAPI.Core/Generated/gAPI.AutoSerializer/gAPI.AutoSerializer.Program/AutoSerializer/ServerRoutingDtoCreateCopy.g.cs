using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerRoutingDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerRoutingDto CreateCopy(this ServerRoutingDto value)
    {
        return new ServerRoutingDto(value.From_FabricManagerId, value.From_FabricConnectionId, value.RequestId, value.ServiceId, value.MethodId, value.FabricConnectionId, value.ClientConnectionId);
    }
}