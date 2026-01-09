namespace gAPI.Ids;

public readonly struct SseServiceMethodId
{
    public SseServiceMethodId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}
