using gAPI.AutoAuth.Server.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoAuth.Server.Helpers;

public static class SharedReferenceFinder
{
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

    public static SharedReference FindByAttribute(string attributeName, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        return allSymbols
             .Where(t =>
                 t.TypeKind == TypeKind.Interface &&
                 t.HasAttribute(attributeName))
             .Select(interfaceSymbol => new SharedReference(interfaceSymbol))
             .FirstOrDefault()
             ?? throw new Exception($"Cannot find type with attribute `{attributeName}`");
    }

    public static SharedReference? TryFindByAttribute(string attributeName, IEnumerable<INamedTypeSymbol> allSymbols)
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

    //public static SharedReference? TryFindByBaseType(SharedReference targetBaseType, IEnumerable<INamedTypeSymbol> allSymbols)
    //{
    //    foreach (var symbol in allSymbols)
    //    {
    //        if (IsExactType(symbol, targetBaseType.FullName))
    //            return TryFindByBaseType(symbol, allSymbols);
    //    }

    //    return null;
    //}
    //public static SharedReference? TryFindByBaseType(INamedTypeSymbol targetBaseType, IEnumerable<INamedTypeSymbol> allSymbols)
    //{
    //    return allSymbols
    //         .Where(t => t.TypeKind == TypeKind.Class && InheritsFrom(t, targetBaseType))
    //         .Select(classSymbol => new SharedReference(classSymbol))
    //         .FirstOrDefault();
    //}
    //private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol targetBaseType)
    //{
    //    var current = type.BaseType;

    //    while (current != null)
    //    {
    //        // Gebruik SymbolEqualityComparer voor een betrouwbare vergelijking in Roslyn
    //        if (SymbolEqualityComparer.Default.Equals(current, targetBaseType))
    //        {
    //            return true;
    //        }
    //        current = current.BaseType;
    //    }

    //    return false;
    //}
    public static SharedReference? TryFindByInterface(SharedReference targetInterface, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        foreach (var symbol in allSymbols)
        {
            if (IsExactType(symbol, targetInterface.FullName))
                return TryFindByInterface(symbol, allSymbols);
        }

        return null;
    }

    public static SharedReference? TryFindByInterface(INamedTypeSymbol targetInterface, IEnumerable<INamedTypeSymbol> allSymbols)
    {
        return allSymbols
             .Where(t => t.TypeKind == TypeKind.Class && ImplementsInterface(t, targetInterface))
             .Select(classSymbol => new SharedReference(classSymbol))
             .FirstOrDefault();
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol targetInterface)
    {
        // AllInterfaces bevat álle interfaces die dit type implementeert, 
        // inclusief interfaces die door basisklassen of andere interfaces worden overgeërfd.
        return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetInterface));
    }

    public static SharedReference? TryFindByBaseTypeNameStart(string baseTypeName, INamedTypeSymbol[] allSymbols)
    {
        foreach (var symbol in allSymbols)
        {
            var baseType = symbol.BaseType;

            while (baseType != null)
            {
                if (baseType.ToDisplayString().StartsWith(baseTypeName))
                {
                    return new SharedReference(symbol);
                }

                baseType = baseType.BaseType;
            }
        }

        return null;
    }
}
