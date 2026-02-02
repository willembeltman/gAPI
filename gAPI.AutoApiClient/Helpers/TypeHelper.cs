using gAPI.AutoApiClient.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.Linq;

namespace gAPI.AutoApiClient.Helpers;

public class TypeHelper : SharedReference
{
    public TypeHelper(ServiceContext dataModel, ITypeSymbol typeSymbol, bool isNullable = false) 
    {
        Debug.WriteLine(typeSymbol.ToString());

        TypeSymbol = typeSymbol;
        DataModel = dataModel;
        IsNullable = isNullable;

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

            IsDateTime = NameInner == "DateTime";
            IsCheckbox = NameInner == "bool" || NameInner == "bool?";
            IsNumber = NameInner == "int" || NameInner == "long" || NameInner == "float" || NameInner == "double";

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

                IsEnum = typeSymbol.TypeKind == TypeKind.Enum;
                IsDateTimeOffset = NameInner == "DateTimeOffset";
                IsDateTime = NameInner == "DateTime";
                IsCheckbox = NameInner == "bool" || NameInner == "bool?";
                IsNumber = NameInner == "int" || NameInner == "long" || NameInner == "float" || NameInner == "double";

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

    public ITypeSymbol TypeSymbol { get; }
    public ServiceContext DataModel { get; }
    public bool IsNullable { get; }
    public bool IsEnum { get; }
    public bool IsDateTimeOffset { get; }
    public bool IsDateTime { get; }
    public bool IsCheckbox { get; }
    public bool IsArray { get; }
    public bool IsNumber { get; }
    public bool IsGenericType { get; }
    public bool IsReferenceType { get; }
    public bool IsValueType { get; }
    public bool IsTask { get; }
    public bool IsTaskT { get; }
    public bool IsBaseResponse { get; }
    public bool IsBaseResponseT { get; }
    public bool IsBaseListResponseT { get; }

    public override string Name
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
    public override string FullName
    {
        get
        {
            if (!IsGenericType)
                return FullNameInner;
            else
                return $"{FullNameInner}<{string.Join(",", UnderlayingTypes.Select(a => a.FullName))}>";
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
    public string NameInner { get; }
    private string? NameEnd { get; }
    public string FullNameInner => $"{Namespace}.{NameInner}";

    public TypeHelper[] UnderlayingTypes { get; }

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


    //public override bool Equals(object obj) => obj is TypeHelper other && Equals(other);
    //public bool Equals(TypeHelper other) => FullName == other.FullName;
    //public static bool operator ==(TypeHelper left, TypeHelper right) => left.Equals(right);
    //public static bool operator !=(TypeHelper left, TypeHelper right) => !left.Equals(right);

    //public override int GetHashCode()
    //{
    //    unchecked // voorkomt overflow exceptions
    //    {
    //        int hash = 17;
    //        hash = hash * 23 + (FullName?.GetHashCode() ?? 0);
    //        hash = hash * 23 + IsNullable.GetHashCode();
    //        return hash;
    //    }
    //}
}