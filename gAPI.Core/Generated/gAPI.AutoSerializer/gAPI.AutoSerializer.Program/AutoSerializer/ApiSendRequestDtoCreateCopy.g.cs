using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ApiSendRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static ApiSendRequestDto CreateCopy(this ApiSendRequestDto value)
    {
        var copy = new ApiSendRequestDto();
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.SessionId = value.SessionId;
        copy.StateData = value.StateData;
        copy.BinaryData = value.BinaryData.ToArray();
        return copy;
    }
}