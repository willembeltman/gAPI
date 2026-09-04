namespace gAPI.Core.Ids;

public record ServiceMethodId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}