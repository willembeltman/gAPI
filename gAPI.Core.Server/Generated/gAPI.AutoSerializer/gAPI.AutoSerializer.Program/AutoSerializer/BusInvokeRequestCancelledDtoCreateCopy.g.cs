using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusInvokeRequestCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static BusInvokeRequestCancelledDto CreateCopy(this BusInvokeRequestCancelledDto value)
    {
        return new BusInvokeRequestCancelledDto(value.Routing, value.Reason);
    }
}