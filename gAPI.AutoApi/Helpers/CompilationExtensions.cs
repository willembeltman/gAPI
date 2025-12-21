using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoApi.Helpers
{
    // Extensie methode om makkelijker alle types te kunnen doorlopen
    public static class CompilationExtensions
    {
        public static bool HasAttribute(this INamedTypeSymbol namedTypeSymbol, string fullAttributeName)
        {
            var attributes = namedTypeSymbol.GetAttributes();
            var hasGenerateApiAttribute = attributes.Any(attr =>
                attr.AttributeClass?.ToDisplayString() == fullAttributeName // volledig gekwalificeerde naam (optioneel)
            );
            return hasGenerateApiAttribute;
        }
        public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol @this)
        {
            foreach (var member in @this.GetMembers())
            {
                if (member is INamespaceSymbol ns)
                {
                    foreach (var type in ns.GetAllTypes())
                    {
                        yield return type;
                    }
                }
                else if (member is INamedTypeSymbol type)
                {
                    yield return type;
                }
            }
        }
    }

}