using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusSendRequestCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static BusSendRequestCancelledDto CreateCopy(this BusSendRequestCancelledDto value)
    {
        return new BusSendRequestCancelledDto(value.Routing, value.Reason);
    }
}