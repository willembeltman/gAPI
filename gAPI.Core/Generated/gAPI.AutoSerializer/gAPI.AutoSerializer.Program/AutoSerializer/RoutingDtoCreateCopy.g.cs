using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class RoutingDtoCreateCopy
{
    [IsCreateCopy]
    public static RoutingDto CreateCopy(this RoutingDto value)
    {
        return new RoutingDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId);
    }
}