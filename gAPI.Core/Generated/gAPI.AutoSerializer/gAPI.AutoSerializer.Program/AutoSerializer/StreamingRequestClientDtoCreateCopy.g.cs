using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class StreamingRequestClientDtoCreateCopy
{
    [IsCreateCopy]
    public static StreamingRequestClientDto CreateCopy(this StreamingRequestClientDto value)
    {
        return new StreamingRequestClientDto(value.Routing, value.ArgumentIndex, value.StreamId, value.StateIsChanged, value.StateData);
    }
}