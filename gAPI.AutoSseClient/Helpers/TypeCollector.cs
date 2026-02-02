using gAPI.AutoSseClient.Models.Configs;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoSseClient.Helpers;

public class TypeCollector
{
    private readonly HashSet<ITypeSymbol> Seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
    private readonly HashSet<ITypeSymbol> Added = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

    internal TypeCollector(ClientConfig config)
    {
        Config = config;
    }

    internal List<INamedTypeSymbol> Dtos { get; } = new List<INamedTypeSymbol>();
    internal List<INamedTypeSymbol> Enums { get; } = new List<INamedTypeSymbol>();
    //public List<INamedTypeSymbol> Services { get; } = new List<INamedTypeSymbol>();
    internal ClientConfig Config { get; }

    internal void Add(ITypeSymbol type)
    {
        if (!Seen.Add(type))
            return;

        switch (type)
        {
            case INamedTypeSymbol named when named.TypeKind == TypeKind.Enum:

                if (IsValidToAdd(named))
                    Enums.Add(named);
                break;

            case INamedTypeSymbol named when named.TypeKind == TypeKind.Class || named.TypeKind == TypeKind.Struct:
                //if (!SkipTypes.Contains(named.ToDisplayString()) &&
                //    !SkipBaseTypes.Contains(named.OriginalDefinition.ToDisplayString()))

                if (IsValidToAdd(named.OriginalDefinition))
                    Dtos.Add(named.OriginalDefinition);

                foreach (var prop in named.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.DeclaredAccessibility == Accessibility.Public))
                {
                    Add(prop.Type);
                }

                if (named.IsGenericType)
                {
                    foreach (var arg in named.TypeArguments)
                        Add(arg);
                }

                break;

            case IArrayTypeSymbol array:
                Add(array.ElementType);
                break;

            case INamedTypeSymbol gen when gen.IsGenericType:
                foreach (var arg in gen.TypeArguments)
                    Add(arg);
                break;
        }
    }

    private bool IsValidToAdd(ITypeSymbol type)
    {
        return
            type != null &&
            Added.Add(type) &&
            type.ContainingNamespace != null &&
            type.ContainingNamespace.ToDisplayString() != null &&
            Config.BaseNamespaces.Any(a => type.ContainingNamespace.ToDisplayString().StartsWith(a));
    }

}