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
        var copy = new InvokeArgumentResponseDto();
        copy.RequestId = value.RequestId;
        copy.ArgumentIndex = value.ArgumentIndex;
        copy.StreamId = value.StreamId;
        copy.IsCompleted = value.IsCompleted;
        copy.BinaryData = value.BinaryData.ToArray();
        return copy;
    }
}