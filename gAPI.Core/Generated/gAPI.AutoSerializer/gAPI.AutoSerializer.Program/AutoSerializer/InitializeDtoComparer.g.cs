using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InitializeDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InitializeDto value, InitializeDto otherValue)
    {
        if (value.SessionId != otherValue.SessionId) return true;
        if (value.StateData != otherValue.StateData) return true;
        return false;
    }
}