namespace gAPI.Core.Ids;

public record UserId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
    public long ToLong()
    {
        if (long.TryParse(Value, out var id) == false)
            throw new Exception("Cannot parse user id to long");
        return id;
    }
    public Guid ToGuid()
    {
        if (Guid.TryParse(Value, out var id) == false)
            throw new Exception("Cannot parse user id to Guid");
        return id;
    }
}