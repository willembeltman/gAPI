namespace gAPI.Ids;

public readonly record struct ConnectionId(long Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}
