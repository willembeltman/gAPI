using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeResponseDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeResponseDto value, InvokeResponseDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.RespondingSessionId != otherValue.RespondingSessionId) return true;
        if (value.ServiceId != otherValue.ServiceId) return true;
        if (value.MethodId != otherValue.MethodId) return true;
        if (value.UserId != otherValue.UserId) return true;
        if (value.SessionId != otherValue.SessionId) return true;
        if (!(value.BinaryData?.AsSpan().SequenceEqual(otherValue.BinaryData) ?? otherValue.BinaryData is null)) return true;
        return false;
    }
}