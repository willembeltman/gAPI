namespace gAPI.Ids;

public readonly record struct SseManagerId(long Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}