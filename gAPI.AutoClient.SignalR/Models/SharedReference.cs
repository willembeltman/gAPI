using Microsoft.CodeAnalysis;

namespace gAPI.AutoHubClient.Models
{
    internal class SharedReference
    {
        public SharedReference()
        {

        }

        public SharedReference(INamedTypeSymbol a)
        {
            Name = a.Name;
            Namespace = a.ContainingNamespace.ToDisplayString();
        }

        public string Name { get; set; }
        public string Namespace { get; set; }
        public string FullName => $"{Namespace}.{Name}";
    }
}