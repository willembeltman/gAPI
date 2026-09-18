using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class DataChunkDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this DataChunkDto value, DataChunkDto otherValue)
    {
        if (value.Data.AsSpan().SequenceEqual(otherValue.Data) == false) return true;
        if (value.Offset != otherValue.Offset) return true;
        return false;
    }
}