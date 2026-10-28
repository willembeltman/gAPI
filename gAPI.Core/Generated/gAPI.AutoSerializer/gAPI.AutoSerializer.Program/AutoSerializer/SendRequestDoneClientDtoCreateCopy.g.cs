using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestDoneClientDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestDoneClientDto CreateCopy(this SendRequestDoneClientDto value)
    {
        return new SendRequestDoneClientDto(value.Routing, value.Cancelled, value.ExceptionMessage, value.StateIsChanged, value.StateData);
    }
}