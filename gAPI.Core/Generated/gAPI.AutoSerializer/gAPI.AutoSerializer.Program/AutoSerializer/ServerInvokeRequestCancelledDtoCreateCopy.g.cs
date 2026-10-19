using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerInvokeRequestCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerInvokeRequestCancelledDto CreateCopy(this ServerInvokeRequestCancelledDto value)
    {
        return new ServerInvokeRequestCancelledDto(value.Routing, value.Reason);
    }
}