using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendGetSessionCookieDataResponseDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SendGetSessionCookieDataResponseDto value, SendGetSessionCookieDataResponseDto otherValue)
    {
        if (value.SessionId != otherValue.SessionId) return true;
        if (value.CookieData != otherValue.CookieData) return true;
        return false;
    }
}