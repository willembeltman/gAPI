using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class InvokeResponseDto
{
    public RequestId RequestId { get; set; } = default!;
    public SessionId RespondingSessionId { get; set; }
    public ServiceId ServiceId { get; set; } = default!;
    public ServiceMethodId MethodId { get; set; } = default!;
    public UserId? UserId { get; set; }
    public SessionId? SessionId { get; set; }
    //public bool StateIsChanged { get; set; }
    //public string? StateData { get; set; }
    public byte[]? BinaryData { get; set; }

    public override string ToString()
    {
        return $"{ServiceId}/{MethodId} #{RequestId}";
    }
}
