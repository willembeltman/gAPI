using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestDoneDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SendRequestDoneDto value, SendRequestDoneDto otherValue)
    {
        if (value.RequestId != otherValue.RequestId) return true;
        return false;
    }
}