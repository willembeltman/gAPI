using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    public string? StateData { get; set; }
}
