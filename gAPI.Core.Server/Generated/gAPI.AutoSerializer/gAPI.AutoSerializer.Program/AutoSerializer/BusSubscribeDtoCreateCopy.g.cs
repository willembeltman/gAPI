using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusSubscribeDtoCreateCopy
{
    [IsCreateCopy]
    public static BusSubscribeDto CreateCopy(this BusSubscribeDto value)
    {
        return new BusSubscribeDto(value.ServiceId, value.BusConnectionId);
    }
}