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
        return new InvokeRequestDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.StateIsChanged, value.StateData, value.BinaryData.ToArray());
    }
}