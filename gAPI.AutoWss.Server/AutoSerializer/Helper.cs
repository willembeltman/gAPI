using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace gAPI.AutoSerializer;

public static class Helper
{
    // --- Helpers ---
    public static uint ComputeFNV1a32(string text)
    {
        const uint fnvOffset = 2166136261;
        const uint fnvPrime = 16777619;

        uint hash = fnvOffset;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= fnvPrime;
        }
        return hash;
    }

    public static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
            yield return type;

        foreach (var subNs in ns.GetNamespaceMembers())
            foreach (var type in GetAllTypes(subNs))
                yield return type;
    }

    public static bool IsPrimitiveOrKnownType(ITypeSymbol type)
    {
        type = GetUnderlayingAndNullable(type).underlyingType;

        if (type.SpecialType is
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_Boolean or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Decimal or
            SpecialType.System_String)
            return true;

        return type.ToDisplayString() == "System.DateTime"
            || type.ToDisplayString() == "System.DateTimeOffset"
            || type.ToDisplayString() == "System.Guid"
            || type.TypeKind == TypeKind.Enum;
    }

    public static bool HasGenerateSerializerAttribute(INamedTypeSymbol type)
    {
        return type.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "gAPI.Core.Attributes.GenerateSerializerAttribute");
    }

    public static bool HasSerializerReadAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsSerializerReadAttribute");
    }
    public static bool HasSerializerWriteAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsSerializerWriteAttribute");
    }
    public static bool HasSpanSerializerLengthAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsSpanSerializerLengthAttribute");
    }

    public static bool HasSpanSerializerReadAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsSpanSerializerReadAttribute");
    }
    public static bool HasSpanSerializerWriteAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsSpanSerializerWriteAttribute");
    }

    public static bool HasComparerAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsComparerAttribute");
    }
    public static bool HasCreateCopyAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsCreateCopyAttribute");
    }
    public static bool HasMultipartFormDataContentSerializerAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsMultipartFormDataContentSerializerAttribute");
    }

    public static PropertyGeneric[] GetProperties(INamedTypeSymbol typeSymbol, bool generic = false)
    {
        var props = typeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p =>
                p.GetMethod != null &&
                p.SetMethod != null &&
                p.GetAttributes().Any(a => a.ToString().EndsWith("NotMappedAttribute")) == false)
            .Select(p => new PropertyGeneric(p, typeSymbol.IsGenericType || generic))
            .ToArray();
        if (typeSymbol.BaseType != null && typeSymbol.BaseType.Name != "Object")
        {
            var baseProps = GetProperties(typeSymbol.BaseType, true);
            return baseProps.Concat(props).ToArray();
        }
        return props;
    }


    public static string GetFullTypeName(ITypeSymbol typeSymbol, Action<INamedTypeSymbol> reg)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            return GetFullTypeName(namedTypeSymbol, reg);
        }
        return typeSymbol.Name;
    }
    public static string GetFullTypeName(INamedTypeSymbol typeSymbol, Action<INamedTypeSymbol> reg)
    {
        reg(typeSymbol);
        if (typeSymbol.IsGenericType)
        {
            var genericArgs = string.Join(", ", typeSymbol.TypeArguments
                .Where(t => t is INamedTypeSymbol)
                .Select(t => GetFullTypeName((t as INamedTypeSymbol)!, reg)));
            return $"{typeSymbol.Name}<{genericArgs}>";
        }
        return typeSymbol.Name;
    }

    public static string GetName(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            return GetName(namedTypeSymbol);
        }
        return typeSymbol.Name;
    }
    public static string GetName(INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.IsGenericType)
        {
            var genericArgs = string.Join("__", namedTypeSymbol.TypeArguments
                .Where(t => t is INamedTypeSymbol)
                .Select(t => GetName((ITypeSymbol)(t as INamedTypeSymbol)!)));
            return $"{namedTypeSymbol.Name}__{genericArgs}";
        }
        return namedTypeSymbol.Name;
    }

    public static (ITypeSymbol underlyingType, bool isNullable, bool isNullableT) GetUnderlayingAndNullable(ITypeSymbol type)
    {
        var underlyingType = type;
        var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
        var isNullableT = false;

        // Nullable<T> detectie
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol nts &&
            nts.TypeArguments.Length == 1)
        {
            underlyingType = nts.TypeArguments[0];
            isNullable = true;
            isNullableT = true;
        }

        return (underlyingType, isNullable, isNullableT);
    }

}