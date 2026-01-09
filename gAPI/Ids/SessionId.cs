namespace gAPI.Ids;

public readonly struct SessionId
{
    public SessionId(string value)
    {
        Value = value;
    }
    public SessionId(Guid value)
    {
        Value = value.ToString();
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}