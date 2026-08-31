using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class InvokeArgumentResponseDto
{
    public RequestId RequestId { get; set; }
    public int ArgumentIndex { get; set; }
    public Guid StreamId { get; set; }
    public bool IsCompleted { get; set; }
    public byte[] BinaryData { get; set; } = [];
}
