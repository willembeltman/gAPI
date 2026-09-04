namespace gAPI.Core.Ids;

public record UserId(string? Value)
{
    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}