using Microsoft.CodeAnalysis;

namespace gAPI.AutoApi.Models
{
    internal class Service
    {
        public Service(Interface @interface, INamedTypeSymbol namedTypeSymbol)
        {
            Interface = @interface;
            NamedTypeSymbol = namedTypeSymbol;

            Name = NamedTypeSymbol.Name;
            FullName = NamedTypeSymbol.ToDisplayString();
            Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();
        }

        public Interface Interface { get; }
        public INamedTypeSymbol NamedTypeSymbol { get; }
        public string Name { get; }
        public string FullName { get; }
        public string Namespace { get; }
    }
}