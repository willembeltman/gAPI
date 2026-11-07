using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusInvokeRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static BusInvokeRequestDto CreateCopy(this BusInvokeRequestDto value)
    {
        return new BusInvokeRequestDto(value.Routing, value.BinaryData.ToArray());
    }
}