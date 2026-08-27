using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class AuthStateDtoCreateCopy
{
    [IsCreateCopy]
    public static AuthStateDto CreateCopy(this AuthStateDto value)
    {
        var copy = new AuthStateDto();
        copy.User = value.User == null ? null : value.User.CreateCopy();
        copy.ForceReconnect = value.ForceReconnect;
        return copy;
    }
}