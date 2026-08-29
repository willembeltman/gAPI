using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeArgumentResponseDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeArgumentResponseDto value, InvokeArgumentResponseDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ArgumentIndex != otherValue.ArgumentIndex) return true;
        if (value.IsCompleted != otherValue.IsCompleted) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        return false;
    }
}