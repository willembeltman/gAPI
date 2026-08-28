using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ClearSessionDtoCreateCopy
{
    [IsCreateCopy]
    public static ClearSessionDto CreateCopy(this ClearSessionDto value)
    {
        return new ClearSessionDto(value.SessionId);
    }
}