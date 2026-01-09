namespace gAPI.Ids;

public readonly struct SseServiceId
{
    public SseServiceId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}