using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ApiInvokeRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static ApiInvokeRequestDto CreateCopy(this ApiInvokeRequestDto value)
    {
        var copy = new ApiInvokeRequestDto();
        copy.RequestId = value.RequestId;
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.SessionId = value.SessionId;
        copy.StateData = value.StateData;
        copy.BinaryData = value.BinaryData.ToArray();
        return copy;
    }
}