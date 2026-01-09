using Microsoft.CodeAnalysis;

namespace gAPI.AutoApiClient.Models;

internal class SharedReference
{
    public SharedReference(ISymbol clientAuthenticationService)
    {
        Symbol = clientAuthenticationService;
        Name = clientAuthenticationService.Name;
        Namespace = clientAuthenticationService.ContainingNamespace.ToDisplayString();
    }

    public ISymbol Symbol { get; }
    public string Name { get; }
    public string Namespace { get; }
    public string FullName => $"{Namespace}.{Name}";
}