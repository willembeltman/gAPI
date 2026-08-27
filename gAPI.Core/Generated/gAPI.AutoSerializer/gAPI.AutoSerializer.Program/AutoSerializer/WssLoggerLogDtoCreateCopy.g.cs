using gAPI.Core.Wss;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Wss;

public static class WssLoggerLogDtoCreateCopy
{
    [IsCreateCopy]
    public static WssLoggerLogDto CreateCopy(this WssLoggerLogDto value)
    {
        var copy = new WssLoggerLogDto();
        copy.Level = value.Level;
        copy.Message = value.Message;
        copy.Category = value.Category;
        copy.Source = value.Source;
        copy.Timestamp = value.Timestamp;
        copy.CorrelationId = value.CorrelationId;
        copy.UserId = value.UserId;
        copy.Data = value.Data == null ? null : value.Data.Select(item1 => item1.CreateCopy()).ToArray();
        copy.StackTrace = value.StackTrace;
        return copy;
    }
}