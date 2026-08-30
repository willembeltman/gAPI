using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeResponseExceptionDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeResponseExceptionDto CreateCopy(this InvokeResponseExceptionDto value)
    {
        var copy = new InvokeResponseExceptionDto();
        copy.RequestId = value.RequestId;
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.SessionId = value.SessionId;
        copy.StateData = value.StateData;
        copy.ExceptionMessage = value.ExceptionMessage;
        return copy;
    }
}