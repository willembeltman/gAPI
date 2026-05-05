namespace gAPI.Core.Attributes;

public class TitleAttribute(string name) : Attribute
{
    public string? Name { get; } = name;
}
