using gAPI.Core.Attributes;
using Microsoft.Extensions.Primitives;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class InitializeDto
{
    public string SessionId { get; set; } = string.Empty;
    public string? StateData { get; set; }
}
