using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendArgumentedRequestDoneDtoCreateCopy
{
    [IsCreateCopy]
    public static SendArgumentedRequestDoneDto CreateCopy(this SendArgumentedRequestDoneDto value)
    {
        var copy = new SendArgumentedRequestDoneDto();
        copy.RequestId = value.RequestId;
        return copy;
    }
}