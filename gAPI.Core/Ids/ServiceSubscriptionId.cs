namespace gAPI.Core.Ids;

public readonly record struct ServiceSubscriptionId(long Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}