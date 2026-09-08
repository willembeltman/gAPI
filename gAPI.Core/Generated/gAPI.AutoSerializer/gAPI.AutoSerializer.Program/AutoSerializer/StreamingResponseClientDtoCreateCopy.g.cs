using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class StreamingResponseClientDtoCreateCopy
{
    [IsCreateCopy]
    public static StreamingResponseClientDto CreateCopy(this StreamingResponseClientDto value)
    {
        return new StreamingResponseClientDto(value.Routing, value.ArgumentIndex, value.StreamId, value.IsCompleted, value.StateIsChanged, value.StateData, value.BinaryData.ToArray());
    }
}