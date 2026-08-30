using gAPI.Core.Ids;

namespace gAPI.Core.Sse;

public class ApiResult
{
    public string? StateData { get; set; }
    public SessionId? SessionId { get; set; }
    public RequestId? RequestId { get; set; }
    public ServiceId? ServiceId { get; set; }
    public ServiceMethodId? MethodId { get; set; }
}
