using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerSendRequestCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerSendRequestCancelledDto CreateCopy(this ServerSendRequestCancelledDto value)
    {
        return new ServerSendRequestCancelledDto(value.Routing, value.Reason);
    }
}