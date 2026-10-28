using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class DataChunkDtoCreateCopy
{
    [IsCreateCopy]
    public static DataChunkDto CreateCopy(this DataChunkDto value)
    {
        var copy = new DataChunkDto();
        copy.Data = value.Data.ToArray();
        copy.Offset = value.Offset;
        return copy;
    }
}