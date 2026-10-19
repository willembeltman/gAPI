using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerInvokeRequestDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this ServerInvokeRequestDto value, ServerInvokeRequestDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.Path != otherValue.Path) return true;
        if (value.QueryString != otherValue.QueryString) return true;
        if (value.IpAddress.AsSpan().SequenceEqual(otherValue.IpAddress) == false) return true;
        if (value.CookieData != otherValue.CookieData) return true;
        if (value.StateData != otherValue.StateData) return true;
        if (value.SessionData != otherValue.SessionData) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        return false;
    }
}