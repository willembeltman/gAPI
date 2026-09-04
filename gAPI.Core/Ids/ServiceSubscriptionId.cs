namespace gAPI.Core.Ids;

public record ServiceSubscriptionId(long Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}