using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusStreamingRequestDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusStreamingRequestDto value, BusStreamingRequestDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.ArgumentIndex != otherValue.ArgumentIndex) return true;
        if (value.StreamId != otherValue.StreamId) return true;
        if (value.Cancelled != otherValue.Cancelled) return true;
        return false;
    }
}