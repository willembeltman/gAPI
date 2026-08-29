using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendGetSessionCookieDataDtoCreateCopy
{
    [IsCreateCopy]
    public static SendGetSessionCookieDataDto CreateCopy(this SendGetSessionCookieDataDto value)
    {
        return new SendGetSessionCookieDataDto(value.SessionId);
    }
}