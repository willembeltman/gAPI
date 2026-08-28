using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class UpdateSessionDtoCreateCopy
{
    [IsCreateCopy]
    public static UpdateSessionDto CreateCopy(this UpdateSessionDto value)
    {
        return new UpdateSessionDto(value.SessionId, value.CookieData);
    }
}