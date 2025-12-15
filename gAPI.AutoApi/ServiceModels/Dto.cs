using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoApi.Models
{
    internal class Dto
    {
        public Dto(ServiceContext dataModel, INamedTypeSymbol namedTypeSymbol)
        {
            NamedTypeSymbol = namedTypeSymbol;

            Name = NamedTypeSymbol.Name;
            FullName = NamedTypeSymbol.ToDisplayString();
            Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

            Properties = NamedTypeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.Name) &&
                    !string.IsNullOrWhiteSpace(p.Type?.ToDisplayString()))
                .Select(propertySymbol => new DtoProperty(dataModel, this, propertySymbol))
                .ToArray();

            IsUser = NamedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUserAttribute");
        }

        public INamedTypeSymbol NamedTypeSymbol { get; }
        public string Name { get; }
        public string FullName { get; }
        public string Namespace { get; }
        public DtoProperty[] Properties { get; }
        public bool IsUser { get; }
    }
}