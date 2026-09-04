using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class StreamingRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static StreamingRequestDto CreateCopy(this StreamingRequestDto value)
    {
        return new StreamingRequestDto(value.Routing, value.ArgumentIndex, value.StreamId);
    }
}