using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeCancelledDto CreateCopy(this InvokeCancelledDto value)
    {
        return new InvokeCancelledDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.Reason);
    }
}