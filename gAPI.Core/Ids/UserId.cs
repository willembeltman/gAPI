namespace gAPI.Ids;

public readonly record struct UserId(string? Value)
{
    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}