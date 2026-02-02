using gAPI.AutoSse.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoSse.Helpers;

internal class TypeHelper
{
    public TypeHelper(ServiceContext dataModel, ITypeSymbol typeSymbol, bool isNullable = false)
    {
        Type = typeSymbol;
        DataModel = dataModel;
        IsNullable = isNullable;
        IsReferenceType = typeSymbol.IsReferenceType;

        var nullable = "";
        var nulleblePrimitive = false;

        // Check of het een Nullable<T> is
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
                typeSymbol = named.TypeArguments[0];
            }
        }

        if (nulleblePrimitive)
        {
            NameInner = typeSymbol.ToDisplayString();
            UnderlayingTypes = [];
        }
        else
        {
            if (isNullable && nullable.Length == 0)
                nullable += "?";

            if (typeSymbol.TypeKind == TypeKind.Array && typeSymbol is IArrayTypeSymbol array)
            {
                IsArray = true;
                NameInner = "";
                NameEnd = "[]" + nullable;
                UnderlayingTypes = [new TypeHelper(dataModel, array.ElementType)];
            }
            else
            {
                NameInner = typeSymbol.Name.Split('`').First();
                NameInner = GetSimpleCsTypeByName(NameInner);
                NameEnd = nullable;
                Namespace = typeSymbol.ContainingNamespace?.ToDisplayString();

                if (typeSymbol is INamedTypeSymbol namedType)
                {
                    IsGenericType = namedType.IsGenericType;
                    UnderlayingTypes = [.. namedType.TypeArguments.Select(t => new TypeHelper(dataModel, t))];
                }
                else
                {
                    UnderlayingTypes = [];
                }

                IsReferenceType = typeSymbol.IsReferenceType;
                IsValueType = typeSymbol.IsValueType;

                if (IsGenericType)
                {
                    IsTaskT = NameInner.StartsWith("Task");
                    IsBaseResponseT = NameInner.StartsWith("BaseResponse");
                    IsBaseListResponseT = NameInner.StartsWith("BaseListResponse");
                }
                else
                {
                    IsTask = NameInner.StartsWith("Task");
                    IsBaseResponse = NameInner.StartsWith("BaseResponse");
                }
            }
        }
    }

    internal ITypeSymbol Type { get; }
    public ServiceContext DataModel { get; }
    public bool IsNullable { get; }
    public bool IsReferenceType { get; }
    public bool IsValueType { get; }
    internal bool IsGenericType { get; }
    public bool IsTask { get; }
    public bool IsTaskT { get; }
    public bool IsBaseResponse { get; }
    public bool IsBaseResponseT { get; }
    public bool IsBaseListResponseT { get; }

    internal string Name
    {
        get
        {
            if (IsArray)
                return $"{UnderlayingTypes[0].Name}{NameEnd}";
            if (IsGenericType)
                return $"{NameInner}<{string.Join(",", UnderlayingTypes.Select(a => a.Name))}>{NameEnd}";
            return $"{NameInner}{NameEnd}";
        }
    }
    internal string FullName
    {
        get
        {
            if (!IsGenericType)
                return FullNameInner;
            else
                return $"{FullNameInner}<{string.Join(",", UnderlayingTypes.Select(a => a.FullName))}>";
        }
    }
    internal string[] Namespaces
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
    internal string NameInner { get; }
    internal bool IsArray { get; }
    private string? NameEnd { get; }
    internal string? Namespace { get; }
    internal string FullNameInner => $"{Namespace}.{NameInner}";
    internal TypeHelper[] UnderlayingTypes { get; }

    internal static string GetSimpleCsTypeByName(string name)
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
}