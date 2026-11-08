using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusSendRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static BusSendRequestDto CreateCopy(this BusSendRequestDto value)
    {
        return new BusSendRequestDto(value.Routing, value.BinaryData.ToArray());
    }
}