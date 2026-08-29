using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ApiInvokeResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static ApiInvokeResponseDto CreateCopy(this ApiInvokeResponseDto value)
    {
        var copy = new ApiInvokeResponseDto();
        copy.RequestId = value.RequestId;
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.SessionData = value.SessionData;
        copy.StateData = value.StateData;
        copy.BinaryData = value.BinaryData.ToArray();
        return copy;
    }
}