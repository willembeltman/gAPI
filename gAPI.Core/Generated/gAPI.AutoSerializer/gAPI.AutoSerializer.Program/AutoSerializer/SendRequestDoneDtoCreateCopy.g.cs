using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestDoneDto CreateCopy(this SendRequestDoneDto value)
    {
        return new SendRequestDoneDto(value.RequestId, value.ServiceId, value.MethodId, value.UserId, value.SessionId, value.StateIsChanged, value.StateData, value.ExceptionMessage);
    }
}