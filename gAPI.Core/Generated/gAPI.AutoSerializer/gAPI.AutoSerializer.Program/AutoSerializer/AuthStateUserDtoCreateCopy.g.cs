using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class AuthStateUserDtoCreateCopy
{
    [IsCreateCopy]
    public static AuthStateUserDto CreateCopy(this AuthStateUserDto value)
    {
        var copy = new AuthStateUserDto();
        copy.Id = value.Id;
        copy.UserName = value.UserName;
        copy.Email = value.Email;
        return copy;
    }
}