using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusStreamingResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static BusStreamingResponseDto CreateCopy(this BusStreamingResponseDto value)
    {
        return new BusStreamingResponseDto(value.Routing, value.ArgumentIndex, value.StreamId, value.IsCompleted, value.IsCancelled, value.ExceptionMessage, value.BinaryData.ToArray());
    }
}