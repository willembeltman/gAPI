using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class DataChunkDto
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Offset { get; set; }
}
