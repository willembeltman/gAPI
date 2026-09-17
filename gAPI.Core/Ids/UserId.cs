namespace gAPI.Core.Ids;

public record UserId(string? Value)
{
    public override string ToString()
    {
        return Value ?? string.Empty;
    }
    public Guid ToGuid()
    {
        if (Guid.TryParse(Value, out Guid id) == false)
            throw new Exception("Cannot parse user id");
        return id;
    }
}