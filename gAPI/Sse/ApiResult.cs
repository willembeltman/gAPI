using gAPI.Ids;

namespace gAPI.Sse;

public class ApiResult
{
    public ApiResult()
    {
    }
    public ApiResult(string? stateData, string? sessionData) : this()
    {
        StateData = stateData;
        SessionData = sessionData;
    }
    public string? StateData { get; set; }
    public string? SessionData { get; set; }
    public RequestId? RequestId { get; set; }
    public ServiceId? ServiceId { get; set; }
    public ServiceMethodId? MethodId { get; set; }
}
