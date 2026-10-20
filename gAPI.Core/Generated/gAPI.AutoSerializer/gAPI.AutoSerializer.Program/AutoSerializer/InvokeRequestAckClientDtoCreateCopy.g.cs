using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestAckClientDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestAckClientDto CreateCopy(this InvokeRequestAckClientDto value)
    {
        return new InvokeRequestAckClientDto(value.Routing, value.StateIsChanged, value.StateData);
    }
}