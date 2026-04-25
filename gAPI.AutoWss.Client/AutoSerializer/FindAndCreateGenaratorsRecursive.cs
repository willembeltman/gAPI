using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoSerializer;

public class FindAndCreateGenaratorsRecursive
{
    public static HashSet<INamedTypeSymbol> FindAndCreateGenerators(
        INamedTypeSymbol[] typesToGenerate,
        IEnumerable<INamedTypeSymbol> customSerializers,
        bool skipRecord = false)
    {
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var type in typesToGenerate)
        {
            GetListRecursive(type, visited, customSerializers, skipRecord);
        }

        return visited;
    }

    private static void GetListRecursive(
        INamedTypeSymbol typeSymbol,
        HashSet<INamedTypeSymbol> visited,
        IEnumerable<INamedTypeSymbol> customSerializers,
        bool skipRecord)
    {
        if (visited.Contains(typeSymbol)) return;
        if (customSerializers.Any(a => SymbolEqualityComparer.Default.Equals(a, typeSymbol))) return;
        visited.Add(typeSymbol);

        foreach (var prop in Helper.GetProperties(typeSymbol)) //typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (prop.Property.SetMethod == null || prop.Property.GetMethod == null) continue;
            if (prop.Property.GetAttributes().Any(a => a.ToString().EndsWith("NotMappedAttribute"))) continue;

            // Pak underlying type als Nullable<T>
            var propType = prop.Property.Type;
            if (propType.OriginalDefinition.ToDisplayString() == "System.Nullable<T>" && propType is INamedTypeSymbol nts && nts.TypeArguments.Length == 1)
            {
                propType = nts.TypeArguments[0];
            }
            // Array !!!!!
            if (propType is IArrayTypeSymbol array)
            {
                if (array.ElementType.Name == "Byte") continue; // Byte array doet de serializer al goed, dus skip
                propType = array.ElementType;
            }

            if (skipRecord && (propType.IsRecord || propType.TypeKind == TypeKind.Struct))
                continue;

            if (!Helper.IsPrimitiveOrKnownType(propType) && propType is INamedTypeSymbol nestedType)
            {
                var customSerializer = customSerializers.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a, nestedType));
                if (customSerializer != null) continue;

                GetListRecursive(nestedType, visited, customSerializers, skipRecord);
            }
        }
    }
}
