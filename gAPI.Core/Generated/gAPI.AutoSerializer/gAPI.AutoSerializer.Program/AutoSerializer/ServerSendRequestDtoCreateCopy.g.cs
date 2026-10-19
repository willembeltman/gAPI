using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerSendRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerSendRequestDto CreateCopy(this ServerSendRequestDto value)
    {
        return new ServerSendRequestDto(value.Routing, value.Path, value.QueryString, value.IpAddress.ToArray(), value.CookieData, value.StateData, value.SessionData, value.BinaryData.ToArray());
    }
}