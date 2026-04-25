using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoSerializer;

public class FindCustomSerializer
{
    public static CustomObject[] GetAllCustomSerializers(INamedTypeSymbol[] allTypes)
    {
        var serializerReadClasses = GetSerializerReadClasses(allTypes);
        var serializerWriteClasses = GetSerializerWriteClasses(allTypes);

        var readerByType = serializerReadClasses
            .ToDictionary(r => r.Type, SymbolEqualityComparer.Default);

        var customSerializers = serializerWriteClasses
            .Select(w => readerByType.ContainsKey(w.Type) ? w :
                throw new Exception($"Cannot find IsSerializerRead method for type {w.Type}"))
            .Select(w => new CustomObject
            {
                Type = w.Type!,
                Writer = w,
                Reader = readerByType[w.Type]
            })
            .ToArray();
        return customSerializers;
    }

    private static CustomObjectMethod[] GetSerializerReadClasses(INamedTypeSymbol[] allTypes)
    {
        return allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                ReadMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasSerializerReadAttribute(m))
                    .ToArray()
            })
            .Where(x => x.ReadMethods.Any())
            .SelectMany(a => a.ReadMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = a.Type,
                Method = method,
                Type = method.ReturnType as INamedTypeSymbol
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();
    }
    private static CustomObjectMethod[] GetSerializerWriteClasses(INamedTypeSymbol[] allTypes)
    {
        return allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                WriteMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasSerializerWriteAttribute(m))
                    .ToArray()
            })
            .Where(x => x.WriteMethods.Any())
            .SelectMany(staticClass => staticClass.WriteMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = staticClass.Type,
                Method = method,
                Type = method.Parameters[1].Type as INamedTypeSymbol
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();
    }

    public static CustomObject[] GetAllCustomSpanSerializers(INamedTypeSymbol[] allTypes)
    {
        var serializerWriteClasses = GetSpanSerializerWriteClasses(allTypes);
        var serializerReadClasses = GetSpanSerializerReadClasses(allTypes);
        var serializerLengthClasses = GetSpanSerializerLengthClasses(allTypes);

        var readerByType = serializerReadClasses
            .ToDictionary(r => r.Type, SymbolEqualityComparer.Default);
        var lengthByType = serializerLengthClasses
            .ToDictionary(r => r.Type, SymbolEqualityComparer.Default);

        var customSpanSerializers = serializerWriteClasses
            .Select(w => readerByType.ContainsKey(w.Type) ? w : 
                throw new Exception($"Cannot find IsSpanSerializerRead method for type {w.Type}"))
            .Select(w => lengthByType.ContainsKey(w.Type) ? w :
                throw new Exception($"Cannot find IsSpanSerializerLength method for type {w.Type}"))
            .Select(w => new CustomObject
            {
                Type = w.Type,
                Writer = w,
                Reader = readerByType[w.Type],
                Length = lengthByType[w.Type]
            })
            .ToArray();
        return customSpanSerializers;
    }

    private static CustomObjectMethod[] GetSpanSerializerLengthClasses(INamedTypeSymbol[] allTypes)
    {
        return allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                LengthMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasSpanSerializerLengthAttribute(m))
                    .ToArray()
            })
            .Where(x => x.LengthMethods.Any())
            .SelectMany(staticClass => staticClass.LengthMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = staticClass.Type,
                Method = method,
                Type = method.Parameters[1].Type as INamedTypeSymbol
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();
    }
    private static CustomObjectMethod[] GetSpanSerializerReadClasses(INamedTypeSymbol[] allTypes)
    {
        return allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                ReadMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasSpanSerializerReadAttribute(m))
                    .ToArray()
            })
            .Where(x => x.ReadMethods.Any())
            .SelectMany(a => a.ReadMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = a.Type,
                Method = method,
                Type = method.ReturnType as INamedTypeSymbol
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();
    }
    private static CustomObjectMethod[] GetSpanSerializerWriteClasses(INamedTypeSymbol[] allTypes)
    {
        return allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                WriteMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasSpanSerializerWriteAttribute(m))
                    .ToArray()
            })
            .Where(x => x.WriteMethods.Any())
            .SelectMany(staticClass => staticClass.WriteMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = staticClass.Type,
                Method = method,
                Type = method.Parameters[2].Type as INamedTypeSymbol
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();
    }

    public static CustomObjectMethod[] GetAllCustomComparers(INamedTypeSymbol[] allTypes)
    {
        var comparerMethods = allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                WriteMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasComparerAttribute(m))
                    .ToArray()
            })
            .Where(x => x.WriteMethods.Any())
            .SelectMany(staticClass => staticClass.WriteMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = staticClass.Type,
                Method = method,
                Type = (method.Parameters[0].Type as INamedTypeSymbol)!
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();

        return comparerMethods;
    }
    public static CustomObjectMethod[] GetAllCustomCreateCopys(INamedTypeSymbol[] allTypes)
    {
        var createCopyMethods = allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                WriteMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasCreateCopyAttribute(m))
                    .ToArray()
            })
            .Where(x => x.WriteMethods.Any())
            .SelectMany(staticClass => staticClass.WriteMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = staticClass.Type,
                Method = method,
                Type = method.Parameters[0].Type as INamedTypeSymbol
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();

        return createCopyMethods;
    }
    public static CustomObjectMethod[] GetAllCustomMultipartFormDataContents(INamedTypeSymbol[] allTypes)
    {
        var methods = allTypes
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.IsStatic)
            .Select(t => new
            {
                Type = t,
                WriteMethods = t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic &&
                        Helper.HasMultipartFormDataContentSerializerAttribute(m))
                    .ToArray()
            })
            .Where(x => x.WriteMethods.Any())
            .SelectMany(staticClass => staticClass.WriteMethods.Select(method => new CustomObjectMethodNull()
            {
                StaticClass = staticClass.Type,
                Method = method,
                Type = method.Parameters[2].Type as INamedTypeSymbol
            }))
            .Where(a => a.Type != null)
            .Select(a => new CustomObjectMethod(a))
            .ToArray();

        return methods;
    }
}
