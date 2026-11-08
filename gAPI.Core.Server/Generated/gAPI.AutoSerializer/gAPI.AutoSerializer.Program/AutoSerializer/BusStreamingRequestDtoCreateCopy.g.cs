using gAPI.Core.Server.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

public static class BusStreamingRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static BusStreamingRequestDto CreateCopy(this BusStreamingRequestDto value)
    {
        return new BusStreamingRequestDto(value.Routing, value.ArgumentIndex, value.StreamId, value.Cancelled);
    }
}