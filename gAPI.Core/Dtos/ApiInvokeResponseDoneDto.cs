using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class ApiInvokeResponseDoneDto
{
    public RequestId RequestId { get; set; }
    public ServiceId ServiceId { get; set; }
    public ServiceMethodId MethodId { get; set; }
    public string? SessionData { get; set; }
    public string? StateData { get; set; }
}
