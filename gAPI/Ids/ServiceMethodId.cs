namespace gAPI.Ids;

public readonly record struct ServiceMethodId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}