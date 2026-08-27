using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class ApiInvokeResponseDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static ApiInvokeResponseDoneDto CreateCopy(this ApiInvokeResponseDoneDto value)
    {
        var copy = new ApiInvokeResponseDoneDto();
        copy.RequestId = value.RequestId;
        copy.ServiceId = value.ServiceId;
        copy.MethodId = value.MethodId;
        copy.SessionData = value.SessionData;
        copy.StateData = value.StateData;
        return copy;
    }
}