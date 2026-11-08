using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusUnsubscribeDtoCreateCopy
{
    [IsCreateCopy]
    public static BusUnsubscribeDto CreateCopy(this BusUnsubscribeDto value)
    {
        return new BusUnsubscribeDto(value.ServiceId, value.BusConnectionId);
    }
}