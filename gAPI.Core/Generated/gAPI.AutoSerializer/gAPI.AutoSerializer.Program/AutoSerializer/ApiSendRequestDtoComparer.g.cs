using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ApiSendRequestDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this ApiSendRequestDto value, ApiSendRequestDto otherValue)
    {
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.MethodId != otherValue.MethodId) return true;
        if (value.SessionId != otherValue.SessionId) return true;
        if (value.StateData != otherValue.StateData) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        return false;
    }
}