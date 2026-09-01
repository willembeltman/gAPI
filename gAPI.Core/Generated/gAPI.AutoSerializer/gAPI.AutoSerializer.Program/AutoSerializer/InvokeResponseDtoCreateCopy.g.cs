using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeResponseDto CreateCopy(this InvokeResponseDto value)
    {
        return new InvokeResponseDto(value.RespondingSessionId, value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.StateIsChanged, value.StateData, value.BinaryData == null ? null : value.BinaryData.ToArray());
    }
}