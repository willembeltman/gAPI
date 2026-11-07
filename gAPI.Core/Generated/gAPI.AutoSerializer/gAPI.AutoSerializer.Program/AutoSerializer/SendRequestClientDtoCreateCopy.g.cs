using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestClientDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestClientDto CreateCopy(this SendRequestClientDto value)
    {
        return new SendRequestClientDto(value.Routing, value.BinaryData.ToArray(), value.StateIsChanged, value.StateData);
    }
}