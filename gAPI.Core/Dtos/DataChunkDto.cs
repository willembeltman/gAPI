namespace gAPI.Core.Dtos;

public class DataChunkDto
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Offset { get; set; }
}
