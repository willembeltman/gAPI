using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeArgumentRequestDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeArgumentRequestDto CreateCopy(this InvokeArgumentRequestDto value)
    {
        var copy = new InvokeArgumentRequestDto();
        copy.RequestId = value.RequestId;
        copy.ArgumentIndex = value.ArgumentIndex;
        copy.StreamId = value.StreamId;
        return copy;
    }
}