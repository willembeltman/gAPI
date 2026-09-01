using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeArgumentResponseDtoCreateCopy
{
    [IsCreateCopy]
    public static InvokeArgumentResponseDto CreateCopy(this InvokeArgumentResponseDto value)
    {
        return new InvokeArgumentResponseDto(value.RequestId, value.ArgumentIndex, value.StreamId, value.IsCompleted, value.BinaryData.ToArray());
    }
}