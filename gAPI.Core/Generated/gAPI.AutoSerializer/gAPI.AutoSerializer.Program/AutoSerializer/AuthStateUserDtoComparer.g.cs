using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class AuthStateUserDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this AuthStateUserDto value, AuthStateUserDto otherValue)
    {
        if (value.Id.IsDifferent(otherValue.Id)) return true;
        if (value.UserName != otherValue.UserName) return true;
        if (value.Email != otherValue.Email) return true;
        if (value.StorageFileUrl != otherValue.StorageFileUrl) return true;
        return false;
    }
}