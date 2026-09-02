using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestCancelledDto CreateCopy(this InvokeRequestCancelledDto value)
    {
        return new InvokeRequestCancelledDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.Reason);
    }
}