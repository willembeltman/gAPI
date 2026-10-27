using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class UnsubscribeDtoCreateCopy
{
    [IsCreateCopy]
    public static UnsubscribeDto CreateCopy(this UnsubscribeDto value)
    {
        return new UnsubscribeDto(value.ServiceId, value.UserId, value.SessionId);
    }
}