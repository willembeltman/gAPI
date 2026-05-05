namespace gAPI.Core.Ids;

public readonly record struct ServiceId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}