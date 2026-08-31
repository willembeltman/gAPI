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
        var copy = new InvokeResponseDoneDto();
        copy.RequestId = value.RequestId;
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.SessionId = value.SessionId;
        copy.UserId = value.UserId;
        return copy;
    }
}