using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class StreamingResponseDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this StreamingResponseDto value, StreamingResponseDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ArgumentIndex != otherValue.ArgumentIndex) return true;
        if (value.StreamId.IsDifferent(otherValue.StreamId)) return true;
        if (value.IsCompleted != otherValue.IsCompleted) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        return false;
    }
}