using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestExceptionDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestExceptionDto CreateCopy(this SendRequestExceptionDto value)
    {
        var copy = new SendRequestExceptionDto();
        copy.RequestId = value.RequestId;
        copy.ExceptionMessage = value.ExceptionMessage;
        return copy;
    }
}