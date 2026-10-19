using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusSendRequestAckDtoCreateCopy
{
    [IsCreateCopy]
    public static BusSendRequestAckDto CreateCopy(this BusSendRequestAckDto value)
    {
        return new BusSendRequestAckDto(value.Routing, value.Exception);
    }
}