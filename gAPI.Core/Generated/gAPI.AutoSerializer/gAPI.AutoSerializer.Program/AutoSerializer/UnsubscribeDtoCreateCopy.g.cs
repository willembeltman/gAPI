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
        var copy = new UnsubscribeDto();
        copy.ServiceId = value.ServiceId;
        copy.UserId = value.UserId;
        copy.SessionId = value.SessionId;
        return copy;
    }
}