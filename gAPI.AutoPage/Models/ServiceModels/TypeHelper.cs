using gAPI.AutoPage.Interfaces;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class TypeHelper : ITypeHelper
{
    public TypeHelper(ServiceContext serviceContext, ITypeSymbol typeSymbol, bool isNullable = false, ITypeSymbol[]? history = null)
    {
        TypeSymbol = typeSymbol;
        ServiceContext = serviceContext;
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
            UnderlayingTypes = [];
            Setup(serviceContext, typeSymbol, history);
            History = history ?? [TypeSymbol];
            return; // Als het een nulleble primitive is
        }

        if (isNullable && nullable.Length == 0)
            nullable += "?";

        if (typeSymbol.TypeKind == TypeKind.Array && typeSymbol is IArrayTypeSymbol array)
        {
            IsArray = true;
            NameInner = "";
            NameEnd = "[]" + nullable;
            UnderlayingTypes = [new TypeHelper(serviceContext, array.ElementType)];
            Setup(serviceContext, typeSymbol, history);
            History = history ?? [TypeSymbol];
            return; // Als het een array stoppen
        }

        NameInner = typeSymbol.Name.Split('`').First();
        NameInner = GetSimpleCsTypeByName(NameInner);
        NameEnd = nullable;
        Namespace = typeSymbol.ContainingNamespace?.ToDisplayString();

        Setup(serviceContext, typeSymbol, history);

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            IsNullable = IsNullable || namedType.NullableAnnotation.HasFlag(NullableAnnotation.Annotated);
            IsGenericType = namedType.IsGenericType;
            UnderlayingTypes = [.. namedType.TypeArguments.Select(t => new TypeHelper(serviceContext, t))];
        }
        else
        {
            UnderlayingTypes = [];
        }

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

        History = history ?? [TypeSymbol];
    }

    void Setup(ServiceContext serviceContext, ITypeSymbol typeSymbol, ITypeSymbol[]? history)
    {
        IsReferenceType = typeSymbol.IsReferenceType;
        IsValueType = typeSymbol.IsValueType;

        IsVoid = typeSymbol.SpecialType == SpecialType.System_Void;
        IsEnum = typeSymbol.TypeKind == TypeKind.Enum;

        IsCheckbox = NameInner == "bool" || NameInner == "bool?";
        IsString = NameInner == "string" || NameInner == "string?";
        IsGuid = NameInner == "Guid" || NameInner == "Guid?";
        IsDateTime = NameInner == "DateTime" || NameInner == "DateTime?";
        IsDateTimeOffset = NameInner == "DateTimeOffset" || NameInner == "DateTimeOffset?";
        IsNumber =
            NameInner == "byte" || NameInner == "byte?" ||
            NameInner == "short" || NameInner == "short?" ||
            NameInner == "int" || NameInner == "int?" ||
            NameInner == "long" || NameInner == "long?" ||
            NameInner == "float" || NameInner == "float?" ||
            NameInner == "double" || NameInner == "double?" ||
            NameInner == "decimal" || NameInner == "decimal?";
        IsPrimitive = IsNumber || IsString ||
            NameInner == "char" || NameInner == "char?" ||
            NameInner == "bool" || NameInner == "bool?";

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (IsEnum)
                EnumHelper = new EnumHelper(namedTypeSymbol);

            IsUser = namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUserAttribute");
            IsAuthorized = namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsAuthorizedAttribute");
            IsEntryPoint = namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsEntryPointAttribute");
            IsICrudEntity = namedTypeSymbol.AllInterfaces.Any(i => i.Name == "ICrudEntity");

            var isJunctionAttr = namedTypeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "IsFileDeleteAttribute");
            if (isJunctionAttr != null)
            {
                if (isJunctionAttr.ConstructorArguments.Length > 1)
                {
                    IsJunction = true;
                    if (isJunctionAttr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                        isJunctionAttr.ConstructorArguments[0].Value is ITypeSymbol targetTypeSymbolLeft)
                    {
                        JunctionLeftRealType = new TypeHelper(serviceContext, targetTypeSymbolLeft);
                    }
                    if (isJunctionAttr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                        isJunctionAttr.ConstructorArguments[0].Value is ITypeSymbol targetTypeSymbolRight)
                    {
                        JunctionRightRealType = new TypeHelper(serviceContext, targetTypeSymbolRight);
                    }
                }
                else
                {
                    // geef warning dat er niet genoeg constructor arguments zijn
                    throw new Exception(
                        $"Junction table '{FullName}' requires two type arguments for left and right types.");
                }
            }
        }
    }

    public ITypeSymbol TypeSymbol { get; }
    public ServiceContext ServiceContext { get; }

    public string NameInner { get; }
    private string? NameEnd { get; }
    public string? Namespace { get; }

    public TypeHelper[] UnderlayingTypes { get; private set; }
    public bool IsNullable { get; private set; }
    public bool IsEnum { get; private set; }
    public bool IsDateTime { get; private set; }
    public bool IsDateTimeOffset { get; private set; }
    public bool IsCheckbox { get; private set; }
    public bool IsArray { get; private set; }
    public bool IsNumber { get; private set; }
    public bool IsPrimitive { get; private set; }
    public bool IsGenericType { get; private set; }
    public bool IsReferenceType { get; private set; }
    public bool IsValueType { get; private set; }
    public bool IsTask { get; private set; }
    public bool IsTaskT { get; private set; }
    public bool IsBaseResponse { get; private set; }
    public bool IsBaseResponseT { get; private set; }
    public bool IsBaseListResponseT { get; private set; }
    public bool IsString { get; private set; }
    public bool IsGuid { get; private set; }
    public bool IsVoid { get; private set; }

    public string Name
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
    public string FullName
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
    public string FullNameInner => $"{Namespace}{(string.IsNullOrWhiteSpace(Namespace) ? "" : ".")}{NameInner}";

    public ITypeSymbol[] History { get; set; }

    ITypeHelper[] ITypeHelper.UnderlayingTypes => UnderlayingTypes;

    public EnumHelper? EnumHelper { get; private set; }
    public bool IsUser { get; private set; }
    public bool IsAuthorized { get; private set; }
    public bool IsEntryPoint { get; private set; }
    public bool IsICrudEntity { get; private set; }
    public bool IsJunction { get; private set; }
    public ITypeHelper? JunctionLeftRealType { get; private set; }
    public ITypeHelper? JunctionRightRealType { get; private set; }

    public TypeHelperProperty[] GetProperties()
    {
        if (TypeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p =>
                !string.IsNullOrWhiteSpace(p.Name) &&
                !string.IsNullOrWhiteSpace(p.Type?.ToDisplayString()) &&
                History.Contains(p.Type, SymbolEqualityComparer.Default)))
        {
            return [];
        }

        return [.. TypeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>
                !string.IsNullOrWhiteSpace(p.Name) &&
                !string.IsNullOrWhiteSpace(p.Type?.ToDisplayString()))
            .Select(propertySymbol => new TypeHelperProperty(ServiceContext, this, propertySymbol, [..History, propertySymbol.Type]))];
    }
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
    public override bool Equals(object obj) => obj is TypeHelper other && Equals(other);
    public bool Equals(TypeHelper other) => FullName == other.FullName;
    //public static bool operator ==(TypeHelper left, TypeHelper right) => left.Equals(right);
    //public static bool operator !=(TypeHelper left, TypeHelper right) => !left.Equals(right);
    //public static bool operator ==(TypeHelper? left, TypeHelper? right) => right == null ? false : left?.Equals(right) == true;
    //public static bool operator !=(TypeHelper? left, TypeHelper? right) => right == null ? true : left?.Equals(right) == false;
    public override int GetHashCode()
    {
        unchecked // voorkomt overflow exceptions
        {
            int hash = 17;
            hash = hash * 23 + (FullName?.GetHashCode() ?? 0);
            hash = hash * 23 + IsNullable.GetHashCode();
            return hash;
        }
    }
    public override string ToString()
    {
        return FullName;
    }

    ITypeHelperProperty[] ITypeHelper.GetProperties()
    {
        return GetProperties();
    }
}