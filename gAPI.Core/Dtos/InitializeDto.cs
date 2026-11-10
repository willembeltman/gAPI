using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    public string? StateData { get; set; }
    public string SessionId { get; set; }
}
