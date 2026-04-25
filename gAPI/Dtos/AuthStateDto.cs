using gAPI.Attributes;

namespace gAPI.Dtos;

[IsStateDto]
[GenerateSerializer]
public class AuthStateDto
{
    public AuthStateUserDto? User { get; set; }
    public bool ForceReconnect { get; set; }
}