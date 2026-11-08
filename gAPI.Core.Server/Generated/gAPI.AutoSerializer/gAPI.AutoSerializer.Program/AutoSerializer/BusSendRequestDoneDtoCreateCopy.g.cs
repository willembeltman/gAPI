using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusSendRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static BusSendRequestDoneDto CreateCopy(this BusSendRequestDoneDto value)
    {
        return new BusSendRequestDoneDto(value.Routing, value.Cancelled, value.ExceptionMessage);
    }
}