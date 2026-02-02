using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoHubClient.Models;

public class SharedReference
{
    public SharedReference() { }
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
    public SharedReference(INamedTypeSymbol a)
    {
        Name = a.Name;
        Namespace = a.ContainingNamespace.ToDisplayString();
    }

    public string Name { get; protected set; } = string.Empty;
    public string? Namespace { get; protected set; }
    public string FullName => $"{Namespace}.{Name}";

    public override string ToString()
    {
        return Name;
    }
}