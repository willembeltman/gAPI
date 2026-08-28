using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class GetSessionCookieDataResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static GetSessionCookieDataResponseDto CreateCopy(this GetSessionCookieDataResponseDto value)
    {
        return new GetSessionCookieDataResponseDto(value.SessionId, value.CookieData);
    }
}