using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestDto CreateCopy(this SendRequestDto value)
    {
        return new SendRequestDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.StateIsChanged, value.StateData, value.BinaryData.ToArray());
    }
}