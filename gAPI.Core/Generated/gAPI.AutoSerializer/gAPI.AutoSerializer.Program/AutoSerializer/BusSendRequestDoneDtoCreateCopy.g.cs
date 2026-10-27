using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusSendRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static BusSendRequestDoneDto CreateCopy(this BusSendRequestDoneDto value)
    {
        return new BusSendRequestDoneDto(value.Routing, value.Cancelled, value.ExceptionMessage);
    }
}