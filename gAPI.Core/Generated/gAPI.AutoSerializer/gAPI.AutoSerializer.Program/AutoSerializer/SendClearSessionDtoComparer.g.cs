using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendClearSessionDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SendClearSessionDto value, SendClearSessionDto otherValue)
    {
        if (value.SessionId != otherValue.SessionId) return true;
        return false;
    }
}