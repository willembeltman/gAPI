using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class UnsubscribeDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this UnsubscribeDto value, UnsubscribeDto otherValue)
    {
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.UserId != otherValue.UserId) return true;
        if (value.SessionId != otherValue.SessionId) return true;
        return false;
    }
}