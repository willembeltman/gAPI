using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestAckDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestAckDto CreateCopy(this InvokeRequestAckDto value)
    {
        return new InvokeRequestAckDto(value.Routing);
    }
}