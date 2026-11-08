using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusInvokeRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static BusInvokeRequestDoneDto CreateCopy(this BusInvokeRequestDoneDto value)
    {
        return new BusInvokeRequestDoneDto(value.Routing, value.Cancelled, value.ExceptionMessage);
    }
}