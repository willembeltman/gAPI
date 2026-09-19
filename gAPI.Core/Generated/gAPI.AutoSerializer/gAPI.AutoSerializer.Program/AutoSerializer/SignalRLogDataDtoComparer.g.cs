using gAPI.Core.Wss;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Wss;

public static class SignalRLogDataDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SignalRLogDataDto value, SignalRLogDataDto otherValue)
    {
        if (value.Key != otherValue.Key) return true;
        if (value.Value != otherValue.Value) return true;
        return false;
    }
}