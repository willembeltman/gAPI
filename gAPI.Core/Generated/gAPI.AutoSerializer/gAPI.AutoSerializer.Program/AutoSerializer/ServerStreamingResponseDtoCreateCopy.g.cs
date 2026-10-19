using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerStreamingResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerStreamingResponseDto CreateCopy(this ServerStreamingResponseDto value)
    {
        return new ServerStreamingResponseDto(value.Routing, value.ArgumentIndex, value.StreamId, value.IsCompleted, value.IsCancelled, value.ExceptionMessage, value.BinaryData.ToArray());
    }
}