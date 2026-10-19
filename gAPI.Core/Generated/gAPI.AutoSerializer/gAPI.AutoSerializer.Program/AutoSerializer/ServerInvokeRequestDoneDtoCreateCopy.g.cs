using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerInvokeRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerInvokeRequestDoneDto CreateCopy(this ServerInvokeRequestDoneDto value)
    {
        return new ServerInvokeRequestDoneDto(value.Routing, value.Cancelled, value.ExceptionMessage);
    }
}