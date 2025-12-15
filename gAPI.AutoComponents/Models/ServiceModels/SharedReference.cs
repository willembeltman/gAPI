using gAPI.AutoComponents.Interfaces;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoComponents.Models.ServiceModels;

public class SharedReference : ISharedReference
{
    public SharedReference(ISymbol symbol)
    {
        Symbol = symbol;
        Name = symbol.Name;
        Namespace = symbol.ContainingNamespace.ToDisplayString();
    }

    public SharedReference(string @namespace, string name)
    {
        Name = name;
        Namespace = @namespace;
    }

    public ISymbol? Symbol { get; }
    public string Name { get; }
    public string Namespace { get; }
    public string FullName => $"{Namespace}.{Name}";
}