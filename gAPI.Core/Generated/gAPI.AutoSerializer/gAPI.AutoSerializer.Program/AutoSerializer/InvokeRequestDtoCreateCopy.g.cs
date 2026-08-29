using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestDto CreateCopy(this InvokeRequestDto value)
    {
        var copy = new InvokeRequestDto();
        copy.RequestId = value.RequestId;
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.UserId = value.UserId;
        copy.SessionId = value.SessionId;
        copy.StateData = value.StateData;
        copy.BinaryData = value.BinaryData.ToArray();
        return copy;
    }
}