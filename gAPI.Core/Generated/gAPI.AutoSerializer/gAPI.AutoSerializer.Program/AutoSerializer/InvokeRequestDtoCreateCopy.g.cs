using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestDto CreateCopy(this InvokeRequestDto value)
    {
        return new InvokeRequestDto(value.Routing, value.StateIsChanged, value.StateData, value.BinaryData.ToArray());
    }
}