using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeArgumentCancelledDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeArgumentCancelledDto value, InvokeArgumentCancelledDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ArgumentIndex != otherValue.ArgumentIndex) return true;
        if (value.StreamId.IsDifferent(otherValue.StreamId)) return true;
        if (value.Reason != otherValue.Reason) return true;
        return false;
    }
}