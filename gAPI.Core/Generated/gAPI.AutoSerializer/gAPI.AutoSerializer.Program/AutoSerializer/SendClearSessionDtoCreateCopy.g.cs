using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendClearSessionDtoCreateCopy
{
    [IsCreateCopy]
    public static SendClearSessionDto CreateCopy(this SendClearSessionDto value)
    {
        return new SendClearSessionDto(value.SessionId);
    }
}