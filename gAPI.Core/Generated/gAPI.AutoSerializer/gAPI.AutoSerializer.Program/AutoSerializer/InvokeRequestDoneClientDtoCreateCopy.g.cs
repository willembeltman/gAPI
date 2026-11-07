using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestDoneClientDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestDoneClientDto CreateCopy(this InvokeRequestDoneClientDto value)
    {
        return new InvokeRequestDoneClientDto(value.Routing, value.Cancelled, value.ExceptionMessage, value.StateIsChanged, value.StateData);
    }
}