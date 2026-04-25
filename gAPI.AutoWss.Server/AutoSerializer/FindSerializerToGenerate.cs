using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoSerializer;

public class FindSerializerToGenerate
{
    public static INamedTypeSymbol[] GetBinarySerializerToGenerate(INamedTypeSymbol[] allTypes)
    {
        var typesToGenerate = allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                Helper.HasGenerateSerializerAttribute(t))
            .ToList();
        return typesToGenerate.ToArray();
    }
}