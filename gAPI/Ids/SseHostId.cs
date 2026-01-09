namespace gAPI.Ids;

public readonly struct SseHostId
{
    public SseHostId(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public override string ToString()
    {
        return Value.ToString();
    }
}