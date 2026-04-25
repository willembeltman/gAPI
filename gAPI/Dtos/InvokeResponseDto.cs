using gAPI.Attributes;
using gAPI.Ids;

namespace gAPI.Dtos;

[GenerateSerializer]
public class InvokeResponseDto
{
    public RequestId RequestId { get; set; } = default!;
    public ServiceId ServiceId { get; set; } = default!;
    public ServiceMethodId MethodId { get; set; } = default!;
    public UserId? UserId { get; set; }
    public SessionId? SessionId { get; set; }
    public byte[]? BinaryData { get; set; }

    public override string ToString()
    {
        return $"{ServiceId}/{MethodId} #{RequestId}";
    }
}
