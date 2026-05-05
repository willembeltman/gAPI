namespace gAPI.Core.Ids;

public readonly record struct FabricHostId(long Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}