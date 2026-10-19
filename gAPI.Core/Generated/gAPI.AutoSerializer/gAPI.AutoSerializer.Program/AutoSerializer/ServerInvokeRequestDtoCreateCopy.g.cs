using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerInvokeRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerInvokeRequestDto CreateCopy(this ServerInvokeRequestDto value)
    {
        return new ServerInvokeRequestDto(value.Routing, value.Path, value.QueryString, value.IpAddress.ToArray(), value.CookieData, value.StateData, value.SessionData, value.BinaryData.ToArray());
    }
}