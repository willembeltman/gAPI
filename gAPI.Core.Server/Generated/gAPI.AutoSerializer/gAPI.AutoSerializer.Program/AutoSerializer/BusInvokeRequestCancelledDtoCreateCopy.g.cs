using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusInvokeRequestCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static BusInvokeRequestCancelledDto CreateCopy(this BusInvokeRequestCancelledDto value)
    {
        return new BusInvokeRequestCancelledDto(value.Routing, value.Reason);
    }
}