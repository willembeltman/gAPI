using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class EnumHelper : IEnumHelper
{
    public EnumHelper(INamedTypeSymbol namedTypeSymbol)
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
    public IEnumChoice[] Choices { get; }
}