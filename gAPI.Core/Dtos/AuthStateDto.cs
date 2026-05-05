using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[IsStateDto]
[GenerateSerializer]
public class AuthStateDto
{
    public AuthStateUserDto? User { get; set; }
    public bool ForceReconnect { get; set; }
}