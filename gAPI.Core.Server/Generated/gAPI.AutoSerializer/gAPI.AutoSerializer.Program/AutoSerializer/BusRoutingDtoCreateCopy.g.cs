using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusRoutingDtoCreateCopy
{
    [IsCreateCopy]
    public static BusRoutingDto CreateCopy(this BusRoutingDto value)
    {
        return new BusRoutingDto(value.FromBusConnectionId, value.RequestId, value.ServiceId, value.MethodId, value.Timeout, value.deliveryMode, value.To, value.ToBusConnectionId, value.ThrowIfNoReceiver);
    }
}