using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusInvokeRequestAckDtoCreateCopy
{
    [IsCreateCopy]
    public static BusInvokeRequestAckDto CreateCopy(this BusInvokeRequestAckDto value)
    {
        return new BusInvokeRequestAckDto(value.Routing);
    }
}