using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class AuthStateDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this AuthStateDto value, AuthStateDto otherValue)
    {
        if (value.User is null)
        {
            if (otherValue.User is not null) return true;
        }
        else
        {
            if (otherValue.User is null) return true;
            if (value.User.IsDifferent(otherValue.User)) return true;
        }
        if (value.ForceReconnect != otherValue.ForceReconnect) return true;
        return false;
    }
}