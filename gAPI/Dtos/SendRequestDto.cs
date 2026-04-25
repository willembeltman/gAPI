using gAPI.Attributes;
using gAPI.Ids;

namespace gAPI.Dtos;

[GenerateSerializer]
public class SendRequestDto
{
    public ServiceId ServiceId { get; set; }
    public ServiceMethodId MethodId { get; set; }
    public UserId? UserId { get; set; }
    public SessionId? SessionId { get; set; }
    public string? StateData { get; set; }
    public byte[] BinaryData { get; set; } = [];

    public override string ToString()
    {
        return $"{ServiceId}/{MethodId}";
    }
}