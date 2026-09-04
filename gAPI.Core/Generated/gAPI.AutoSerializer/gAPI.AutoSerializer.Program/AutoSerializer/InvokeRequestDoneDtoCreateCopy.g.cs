using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeRequestDoneDto CreateCopy(this InvokeRequestDoneDto value)
    {
        return new InvokeRequestDoneDto(value.Routing, value.StreamIds.Select(item1 => item1).ToArray());
    }
}