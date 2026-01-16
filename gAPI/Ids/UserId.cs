namespace gAPI.Ids;

public readonly struct UserId
{
    public UserId(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    public override string ToString()
    {
        return Value ?? "NULL";
    }
}