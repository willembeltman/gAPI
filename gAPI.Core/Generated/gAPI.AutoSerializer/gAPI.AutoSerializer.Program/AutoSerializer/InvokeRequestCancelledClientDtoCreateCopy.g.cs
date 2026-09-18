using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestCancelledClientDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestCancelledClientDto CreateCopy(this InvokeRequestCancelledClientDto value)
    {
        return new InvokeRequestCancelledClientDto(value.Routing, value.Reason, value.StateIsChanged, value.StateData);
    }
}