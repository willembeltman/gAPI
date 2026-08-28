using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class GetSessionCookieDataDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this GetSessionCookieDataDto value, GetSessionCookieDataDto otherValue)
    {
        if (value.SessionId != otherValue.SessionId) return true;
        return false;
    }
}