using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class UpdateSessionDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this UpdateSessionDto value, UpdateSessionDto otherValue)
    {
        if (value.SessionId != otherValue.SessionId) return true;
        if (value.CookieData != otherValue.CookieData) return true;
        return false;
    }
}