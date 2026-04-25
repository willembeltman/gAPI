using gAPI.AutoApiServer.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace gAPI.AutoApiServer.Helpers;

public static class SharedReferenceFinder
{
    public static SharedReference? TryFindStart(string typeFullName, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        foreach (var symbol in allSymbols)
        {
            if (symbol.ToString().StartsWith(typeFullName))
                return new SharedReference(symbol);
        }
        return null;
    }
    public static SharedReference? TryFind(string typeFullName, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        foreach (var symbol in allSymbols)
        {
            if (IsExactType(symbol, typeFullName))
                return new SharedReference(symbol);
        }
        return null;
    }
    public static SharedReference Find(string typeFullName, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        foreach (var symbol in allSymbols)
        {
            if (IsExactType(symbol, typeFullName))
                return new SharedReference(symbol);
        }

        throw new Exception($"Cannot find type '{typeFullName}', please add gAPI reference to your project.");
    }
    public static bool IsExactType(INamedTypeSymbol symbol, string fullName)
    {
        return symbol.ToDisplayString(FullNameFormat) == fullName;
    }

    public static SharedReference FindByAttribute(string attributeName, INamedTypeSymbol[] allSymbols)
    {
        return allSymbols
             .Where(t => t.HasAttribute(attributeName))
             .Select(interfaceSymbol => new SharedReference(interfaceSymbol))
             .FirstOrDefault()
             ?? throw new Exception($"Cannot find type with attribute `{attributeName}`");
    }

    public static SharedReference? TryFindByAttribute(string attributeName, INamedTypeSymbol[] allSymbols)
    {
        return allSymbols
             .Where(t => t.HasAttribute(attributeName))
             .Select(interfaceSymbol => new SharedReference(interfaceSymbol))
             .FirstOrDefault();
    }

    public static readonly SymbolDisplayFormat FullNameFormat =
        new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

}
