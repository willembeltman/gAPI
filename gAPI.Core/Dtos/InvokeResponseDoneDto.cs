using gAPI.Attributes;
using gAPI.Ids;

namespace gAPI.Dtos;

[GenerateSerializer]
public class InvokeResponseDoneDto
{
    public RequestId RequestId { get; set; }
    public ServiceId ServiceId { get; set; }
    public ServiceMethodId MethodId { get; set; }
}
