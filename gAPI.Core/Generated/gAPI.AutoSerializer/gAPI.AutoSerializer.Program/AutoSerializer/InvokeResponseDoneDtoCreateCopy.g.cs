using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeResponseDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeResponseDoneDto CreateCopy(this InvokeResponseDoneDto value)
    {
        return new InvokeResponseDoneDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.ExceptionMessage);
    }
}