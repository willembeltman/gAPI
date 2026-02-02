using gAPI.AutoComponent.Interfaces;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoComponent.Models;

public class SharedReference : ISharedReference
{
    public SharedReference(string fullName)
    {
        Name = fullName.Split('.').Last();
        Namespace = fullName.Substring(0, fullName.Length - Name.Length - 1);
    }
    public SharedReference(string @namespace, string name)
    {
        Name = name;
        Namespace = @namespace;
    }
    public SharedReference(ISymbol symbol)
    {
        Symbol = symbol;
        Name = symbol.Name;
        Namespace = symbol.ContainingNamespace.ToDisplayString();
    }


    public ISymbol? Symbol { get; }
    public string Name { get; protected set; }
    public string Namespace { get; protected set; }
    public string FullName => $"{Namespace}.{Name}";

    public override string ToString()
    {
        return Name;
    }
}