namespace gAPI.AutoPage.SimpleRazorCompiler;

public class NodeAttribute
{
    public NodeAttribute(string name, string? value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public string? Value { get; }
}
