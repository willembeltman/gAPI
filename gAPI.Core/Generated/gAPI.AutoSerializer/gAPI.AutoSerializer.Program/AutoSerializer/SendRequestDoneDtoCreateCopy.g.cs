using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static SendRequestDoneDto CreateCopy(this SendRequestDoneDto value)
    {
        var copy = new SendRequestDoneDto();
        copy.RequestId = value.RequestId;
        return copy;
    }
}