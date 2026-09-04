namespace gAPI.Core.Ids;

public record ServiceId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}