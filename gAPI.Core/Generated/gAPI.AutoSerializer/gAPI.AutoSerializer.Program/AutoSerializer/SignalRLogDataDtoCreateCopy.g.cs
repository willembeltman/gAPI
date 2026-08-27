using gAPI.Core.Wss;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Wss;

public static class SignalRLogDataDtoCreateCopy
{
    [IsCreateCopy]
    public static SignalRLogDataDto CreateCopy(this SignalRLogDataDto value)
    {
        var copy = new SignalRLogDataDto();
        copy.Key = value.Key;
        copy.Value = value.Value;
        return copy;
    }
}