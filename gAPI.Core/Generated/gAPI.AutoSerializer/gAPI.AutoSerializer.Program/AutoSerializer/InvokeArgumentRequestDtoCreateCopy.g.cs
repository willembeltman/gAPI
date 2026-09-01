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
        return new InvokeArgumentRequestDto(value.RequestId, value.ArgumentIndex, value.StreamId);
    }
}