namespace gAPI.Authentication;

public class AuthenticationInitializeResult
{
    public bool Authenticated { get; set; }
    public bool Forbidden { get; set; } = false;
    public string? ForbiddenReason { get; set; } = null;
}