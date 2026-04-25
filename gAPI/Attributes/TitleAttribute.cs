namespace gAPI.Attributes;

public class TitleAttribute(string name) : Attribute
{
    public string? Name { get; } = name;
}
