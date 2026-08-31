using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class InvokeResponseDoneDto
{
    public InvokeResponseDoneDto()
    {

    }
    public RequestId RequestId { get; set; }
    public ServiceId ServiceId { get; set; }
    public ServiceMethodId MethodId { get; set; }
    public SessionId? SessionId { get; set; }
    public UserId? UserId { get; set; }
}
