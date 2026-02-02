using gAPI.AutoComponent.Interfaces;

namespace gAPI.CodeGen.Frontend.Models;

public class SharedReference : ISharedReference
{
    public SharedReference(string fullName)
    {
        Name = fullName.Split('.').Last();
        Namespace = fullName.Substring(0, fullName.Length - Name.Length - 1);
    }
    public SharedReference(string @namespace, string name)
    {
        Namespace = @namespace;
        Name = name;
    }
    public SharedReference(Type type)
    {
        Name = type.Name.Split(new char[] { '`' }).First();
        Namespace = type.Namespace;
    }

    public string Name { get; protected set; }
    public string? Namespace { get; protected set; }
    public string FullName => $"{Namespace}.{Name}";

    public override string ToString()
    {
        return Name;
    }
}