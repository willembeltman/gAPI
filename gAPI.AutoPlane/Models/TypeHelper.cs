using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPlane.Models;

public class TypeHelper : SharedReference
{
    public TypeHelper(ServiceContext dataModel, ITypeSymbol typeSymbol, bool isNullable = false)
    {
        Type = typeSymbol;
        ServiceContext = dataModel;

        var nullable = "";
        var nulleblePrimitive = false;

        // 1. Check of het een Nullable<T> (ValueType, zoals int?) is
        while (typeSymbol.OriginalDefinition is INamedTypeSymbol named &&
               named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            if (named.TypeArguments[0].Name == "T")
            {
                nulleblePrimitive = true;
                break;
            }
            else
            {
                nullable += "?";
                isNullable = true;
                typeSymbol = named.TypeArguments[0];
            }
        }

        // 2. Check Roslyn's NullableAnnotation op het typeSymbol zelf (voor Reference Types zoals MyClass?)
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            isNullable = true;
        }

        IsNullable = isNullable;

        if (nulleblePrimitive)
        {
            NameInner = typeSymbol.ToDisplayString();
            IsDateTime = NameInner == "DateTime";
            IsDateTimeOffset = NameInner == "DateTimeOffset";
            UnderlayingTypes = [];
        }
        else
        {
            if (IsNullable && nullable.Length == 0)
                nullable += "?";

            if (typeSymbol.TypeKind == TypeKind.Array && typeSymbol is IArrayTypeSymbol array)
            {
                IsArray = true;
                NameInner = "";
                NameEnd = "[]" + nullable;

                // Check of de elementen in de array nullable zijn (bijv. MyClass?[])
                var elementNullableAnnotated = array.ElementNullableAnnotation == NullableAnnotation.Annotated;
                UnderlayingTypes = [new TypeHelper(dataModel, array.ElementType, elementNullableAnnotated)];
            }
            else
            {
                NameInner = typeSymbol.Name.Split('`').First();
                NameInner = GetSimpleCsTypeByName(NameInner);
                NameEnd = nullable; // Nu bevat 'nullable' wél het vraagteken!
                Namespace = typeSymbol.ContainingNamespace?.ToDisplayString();

                if (typeSymbol is INamedTypeSymbol namedType)
                {
                    IsGenericType = namedType.IsGenericType;

                    // Combineer TypeArguments met TypeArgumentNullableAnnotations
                    UnderlayingTypes = namedType.TypeArguments
                        .Select((t, index) =>
                        {
                            var argNullableAnnotation = t.NullableAnnotation == NullableAnnotation.Annotated ||
                                                (index < namedType.TypeArgumentNullableAnnotations.Length &&
                                                 namedType.TypeArgumentNullableAnnotations[index] == NullableAnnotation.Annotated);
                            return new TypeHelper(dataModel, t, argNullableAnnotation);
                        })
                        .ToArray();
                }
                else
                {
                    UnderlayingTypes = [];
                }

                IsReferenceType = typeSymbol.IsReferenceType;
                IsValueType = typeSymbol.IsValueType;

                IsDateTime = NameInner == "DateTime";
                IsDateTimeOffset = NameInner == "DateTimeOffset";
                IsCancellationToken = NameInner == "CancellationToken";

                if (IsGenericType)
                {
                    IsTaskT = NameInner.StartsWith("Task");
                    IsList = NameInner.StartsWith("List");
                    IsIEnumerable = NameInner.StartsWith("IEnumerable");
                    IsIAsyncEnumerable = NameInner.StartsWith("IAsyncEnumerable");
                    IsBaseResponseT = NameInner.StartsWith("BaseResponse");
                    IsBaseListResponseT = NameInner.StartsWith("BaseListResponse");
                    IsHubResultT = NameInner.StartsWith("HubResultT");
                }
                else
                {
                    IsTask = NameInner.StartsWith("Task");
                    IsBaseResponse = NameInner.StartsWith("BaseResponse");
                }
            }
        }
    }

    public ITypeSymbol Type { get; }
    public ServiceContext ServiceContext { get; }
    public bool IsNullable { get; }
    public bool IsGenericType { get; }
    public bool IsReferenceType { get; }
    public bool IsValueType { get; }
    public bool IsTask { get; }
    public bool IsTaskT { get; }
    public bool IsArray { get; }
    public bool IsList { get; }
    public bool IsIEnumerable { get; }
    public bool IsIAsyncEnumerable { get; }
    public bool IsBaseResponse { get; }
    public bool IsBaseResponseT { get; }
    public bool IsBaseListResponseT { get; }
    public bool IsCancellationToken { get; }
    public bool IsHubResultT { get; }
    public bool IsDateTime { get; }
    public bool IsDateTimeOffset { get; }
    public string NameInner { get; }
    private string? NameEnd { get; }
    public TypeHelper[] UnderlayingTypes { get; }

    public bool IsVoid => Type?.SpecialType == SpecialType.System_Void;
    public bool IsProperty => UnderlayingTypes.Length == 0 || IsValueType || IsArray || IsDateTime || IsDateTimeOffset;

    public override string Name
    {
        get
        {
            if (IsArray)
                return $"{UnderlayingTypes[0].Name}{NameEnd}";
            if (IsGenericType)
                return $"{NameInner}<{string.Join(", ", UnderlayingTypes.Select(a => a.Name))}>{NameEnd}";
            return $"{NameInner}{NameEnd}";
        }
    }

    public override string FullName
    {
        get
        {
            if (!IsGenericType)
                return FullNameInner;
            else
                return $"{FullNameInner}<{string.Join(", ", UnderlayingTypes.Select(a => a.FullName))}>";
        }
    }

    public string[] Namespaces
    {
        get
        {
            if (Namespace == null)
            {
                return [.. UnderlayingTypes.SelectMany(a => a.Namespaces)];
            }

            if (!IsGenericType)
                return [Namespace];

            var list = UnderlayingTypes.SelectMany(a => a.Namespaces).ToList();
            list.Insert(0, Namespace);
            return [.. list];
        }
    }

    public string FullNameInner => $"{Namespace}.{NameInner}";

    public static string GetSimpleCsTypeByName(string name)
    {
        return name switch
        {
            "Int64" => "long",
            "Int32" => "int",
            "String" => "string",
            "Double" => "double",
            "Boolean" => "bool",
            "Guid" => "Guid",
            "DateTime" => "DateTime",
            "Byte" => "byte",
            _ => name,
        };
    }

    public override string ToString()
    {
        return Name;
    }

    public IEnumerable<TypeHelperProperty> GetProperties()
    {
        return Type
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>
                !string.IsNullOrWhiteSpace(p.Name) &&
                !string.IsNullOrWhiteSpace(p.Type?.ToDisplayString()))
            .Select(propertySymbol => new TypeHelperProperty(this, propertySymbol))
            .ToArray();
    }
}
