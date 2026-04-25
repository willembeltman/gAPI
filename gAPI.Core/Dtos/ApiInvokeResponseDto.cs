using gAPI.Attributes;
using gAPI.Ids;

namespace gAPI.Dtos;

[GenerateSerializer]
public class ApiInvokeResponseDto
{
    public RequestId RequestId { get; set; }
    public ServiceId ServiceId { get; set; }
    public ServiceMethodId MethodId { get; set; }
    public string SessionData { get; set; } = string.Empty;
    public string? StateData { get; set; }
    public byte[] BinaryData { get; set; } = [];
    public override string ToString()
    {
        return $"{ServiceId}/{MethodId} #{RequestId}";
    }
}