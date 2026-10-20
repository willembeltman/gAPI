using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class BusSendRequestDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this BusSendRequestDto value, BusSendRequestDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.BinaryData.AsSpan().SequenceEqual(otherValue.BinaryData) == false) return true;
        return false;
    }
}