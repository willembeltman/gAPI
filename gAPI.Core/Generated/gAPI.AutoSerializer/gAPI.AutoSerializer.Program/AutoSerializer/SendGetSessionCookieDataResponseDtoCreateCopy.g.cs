using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendGetSessionCookieDataResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static SendGetSessionCookieDataResponseDto CreateCopy(this SendGetSessionCookieDataResponseDto value)
    {
        return new SendGetSessionCookieDataResponseDto(value.SessionId, value.CookieData);
    }
}