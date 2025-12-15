using Microsoft.CodeAnalysis;

namespace gAPI.AutoClient.ServiceModels
{
    internal class Client
    {
        public Client(Interface @interface, INamedTypeSymbol namedTypeSymbol)
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