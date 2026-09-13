using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class StreamingRequestClientDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this StreamingRequestClientDto value, StreamingRequestClientDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.ArgumentIndex != otherValue.ArgumentIndex) return true;
        if (value.StreamId != otherValue.StreamId) return true;

        if (value.Cancelled != otherValue.Cancelled) return true;
        if (value.StateIsChanged != otherValue.StateIsChanged) return true;
        if (value.StateData != otherValue.StateData) return true;
        return false;
    }
}