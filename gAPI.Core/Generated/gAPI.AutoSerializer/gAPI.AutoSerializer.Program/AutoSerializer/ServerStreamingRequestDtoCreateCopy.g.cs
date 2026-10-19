using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ServerStreamingRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static ServerStreamingRequestDto CreateCopy(this ServerStreamingRequestDto value)
    {
        return new ServerStreamingRequestDto(value.Routing, value.ArgumentIndex, value.StreamId, value.Cancelled);
    }
}