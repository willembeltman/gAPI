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
        return new SendRequestDoneDto(value.Routing, value.StateIsChanged, value.StateData, value.ExceptionMessage);
    }
}