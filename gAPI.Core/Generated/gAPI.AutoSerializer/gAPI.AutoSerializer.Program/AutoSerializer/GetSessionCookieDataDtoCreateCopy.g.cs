using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class GetSessionCookieDataDtoCreateCopy
{
    [IsCreateCopy]
    public static GetSessionCookieDataDto CreateCopy(this GetSessionCookieDataDto value)
    {
        return new GetSessionCookieDataDto(value.SessionId);
    }
}