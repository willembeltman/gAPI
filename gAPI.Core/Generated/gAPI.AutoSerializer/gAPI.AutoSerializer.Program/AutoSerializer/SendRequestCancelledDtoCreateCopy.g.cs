using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestCancelledDto CreateCopy(this SendRequestCancelledDto value)
    {
        return new SendRequestCancelledDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.Reason);
    }
}