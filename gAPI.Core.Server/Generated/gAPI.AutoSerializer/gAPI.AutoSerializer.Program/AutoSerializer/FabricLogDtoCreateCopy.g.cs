using gAPI.Core.Wss;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Wss;

public static class FabricLogDtoCreateCopy
{
    [IsCreateCopy]
    public static FabricLogDto CreateCopy(this FabricLogDto value)
    {
        var copy = new FabricLogDto();
        copy.Level = value.Level;
        copy.Message = value.Message;
        copy.Category = value.Category;
        copy.Source = value.Source;
        copy.Timestamp = value.Timestamp;
        copy.CorrelationId = value.CorrelationId;
        copy.UserId = value.UserId;
        copy.StackTrace = value.StackTrace;
        return copy;
    }
}