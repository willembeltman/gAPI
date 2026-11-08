using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusInvokeRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static BusInvokeRequestDto CreateCopy(this BusInvokeRequestDto value)
    {
        return new BusInvokeRequestDto(value.Routing, value.BinaryData.ToArray());
    }
}