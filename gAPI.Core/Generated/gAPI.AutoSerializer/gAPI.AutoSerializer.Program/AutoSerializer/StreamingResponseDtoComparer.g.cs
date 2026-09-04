using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class StreamingResponseDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this StreamingResponseDto value, StreamingResponseDto otherValue)
    {
        if (value.ResponseFromSessionId != otherValue.ResponseFromSessionId) return true;
        if (value.Routing != otherValue.Routing) return true;
        if (value.ArgumentIndex != otherValue.ArgumentIndex) return true;
        if (value.StreamId != otherValue.StreamId) return true;
        if (value.IsCompleted != otherValue.IsCompleted) return true;
        if (value.StateIsChanged != otherValue.StateIsChanged) return true;
        if (value.StateData != otherValue.StateData) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        return false;
    }
}