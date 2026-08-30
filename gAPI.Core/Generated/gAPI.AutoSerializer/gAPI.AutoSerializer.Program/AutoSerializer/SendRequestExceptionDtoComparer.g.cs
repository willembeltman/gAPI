using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestExceptionDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SendRequestExceptionDto value, SendRequestExceptionDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        if (value.ExceptionMessage != otherValue.ExceptionMessage) return true;
        return false;
    }
}