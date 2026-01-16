namespace gAPI.CodeGen.Backend.Models;

public class SharedReference
{
    public SharedReference()
    {

    }

    public SharedReference(Type type)
    {
        Name = type.Name.Split(new char[] { '`' }).First();
        Namespace = type.Namespace;
    }

    public SharedReference(string @namespace, string name)
    {
        Namespace = @namespace;
        Name = name;
    }

    public string Name { get; protected set; } = string.Empty;
    public string? Namespace { get; protected set; }
    public string FullName => $"{Namespace}.{Name}";
}