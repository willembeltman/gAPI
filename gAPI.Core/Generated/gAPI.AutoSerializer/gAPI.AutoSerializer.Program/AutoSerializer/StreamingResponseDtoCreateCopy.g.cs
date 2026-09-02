using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class StreamingResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static StreamingResponseDto CreateCopy(this StreamingResponseDto value)
    {
        return new StreamingResponseDto(value.RequestId, value.ArgumentIndex, value.StreamId, value.IsCompleted, value.BinaryData.ToArray());
    }
}