namespace gAPI.Ids;

public readonly struct FabricHostId
{
    public FabricHostId(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public override string ToString()
    {
        return Value.ToString();
    }
}