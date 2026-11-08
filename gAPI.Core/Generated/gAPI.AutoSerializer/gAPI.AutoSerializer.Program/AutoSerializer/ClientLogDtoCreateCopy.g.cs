using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ClientLogDtoCreateCopy
{
    [IsCreateCopy]
    public static ClientLogDto CreateCopy(this ClientLogDto value)
    {
        var copy = new ClientLogDto();
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