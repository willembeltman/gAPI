using gAPI.AutoComponent.Interfaces;

namespace gAPI.CodeGen.Frontend.Models;

public class SharedReference : ISharedReference
{
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

    public string Name { get; }
    public string? Namespace { get; }
    public string FullName => $"{Namespace}.{Name}";
}