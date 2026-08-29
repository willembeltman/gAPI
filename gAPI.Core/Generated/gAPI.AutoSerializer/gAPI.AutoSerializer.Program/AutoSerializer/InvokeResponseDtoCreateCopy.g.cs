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
        var copy = new InvokeResponseDto();
        copy.RequestId = value.RequestId;
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.UserId = value.UserId;
        copy.SessionId = value.SessionId;
        copy.BinaryData = value.BinaryData == null ? null : value.BinaryData.ToArray();
        return copy;
    }
}