using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class SendRequestDoneDto
{
    public RequestId RequestId { get; set; }
    //public bool StateIsChanged { get; set; }
    //public string? StateData { get; set; }
}