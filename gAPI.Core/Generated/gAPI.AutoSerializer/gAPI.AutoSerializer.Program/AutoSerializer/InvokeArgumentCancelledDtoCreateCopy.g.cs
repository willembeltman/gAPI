using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeArgumentCancelledDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeArgumentCancelledDto CreateCopy(this InvokeArgumentCancelledDto value)
    {
        return new InvokeArgumentCancelledDto(value.RequestId, value.ArgumentIndex, value.StreamId, value.Reason);
    }
}