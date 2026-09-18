using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendGetSessionCookieDataDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SendGetSessionCookieDataDto value, SendGetSessionCookieDataDto otherValue)
    {
        if (value.SessionId != otherValue.SessionId) return true;
        return false;
    }
}