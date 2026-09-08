using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestClientDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestClientDto CreateCopy(this InvokeRequestClientDto value)
    {
        return new InvokeRequestClientDto(value.Routing, value.StateIsChanged, value.StateData, value.BinaryData.ToArray());
    }
}