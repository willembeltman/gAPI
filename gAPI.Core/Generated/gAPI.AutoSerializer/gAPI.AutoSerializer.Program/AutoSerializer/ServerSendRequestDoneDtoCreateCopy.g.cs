using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerSendRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerSendRequestDoneDto CreateCopy(this ServerSendRequestDoneDto value)
    {
        return new ServerSendRequestDoneDto(value.Routing, value.Cancelled, value.ExceptionMessage);
    }
}