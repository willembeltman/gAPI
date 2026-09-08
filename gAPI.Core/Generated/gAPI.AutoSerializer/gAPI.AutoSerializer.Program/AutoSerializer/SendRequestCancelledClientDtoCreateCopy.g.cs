using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestCancelledClientDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestCancelledClientDto CreateCopy(this SendRequestCancelledClientDto value)
    {
        return new SendRequestCancelledClientDto(value.Routing, value.StateIsChanged, value.StateData, value.Reason);
    }
}