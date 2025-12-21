using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoApi.SignalR.Models
{
    internal class EnumDto
    {
        public EnumDto(INamedTypeSymbol namedTypeSymbol)
        {
            NamedTypeSymbol = namedTypeSymbol;

            Name = NamedTypeSymbol.Name;
            FullName = NamedTypeSymbol.ToDisplayString();
            Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

            Choices = NamedTypeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.ConstantValue != null)
                .Select(fieldSymbol => new EnumChoice(this, fieldSymbol))
                .ToArray();
        }

        public INamedTypeSymbol NamedTypeSymbol { get; }
        public string Name { get; }
        public string FullName { get; }
        public string Namespace { get; }
        public EnumChoice[] Choices { get; }
    }
}