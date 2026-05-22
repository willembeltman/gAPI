using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Tester;

[GenerateSerializer]
public class ApiInvokeRequestDto2
{
    public RequestId RequestId { get; set; } = default!;
    public ServiceId ServiceId { get; set; } = default!;
    public ServiceMethodId MethodId { get; set; } = default!;
    public SessionId? SessionId { get; set; }
    public string? StateData { get; set; }
    public string Data { get; set; } = string.Empty;
    public byte[] BinaryData { get; set; } = [];
    public override string ToString()
    {
        return $"{ServiceId}/{MethodId} #{RequestId}";
    }
}
