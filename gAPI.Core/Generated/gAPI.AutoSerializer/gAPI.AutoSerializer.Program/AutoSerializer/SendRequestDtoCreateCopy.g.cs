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
        var copy = new SendRequestDto();
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