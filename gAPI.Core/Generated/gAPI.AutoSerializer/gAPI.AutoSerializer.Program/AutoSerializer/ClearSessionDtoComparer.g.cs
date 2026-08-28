using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ClearSessionDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this ClearSessionDto value, ClearSessionDto otherValue)
    {
        if (value.SessionId != otherValue.SessionId) return true;
        return false;
    }
}