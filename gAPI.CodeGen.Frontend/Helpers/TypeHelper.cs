using gAPI.AutoComponent.Interfaces;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace gAPI.CodeGen.Frontend.Helpers;

public class TypeHelper : ITypeHelper
{
    public TypeHelper(Type type, bool isNullable = false)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        IsNullable = isNullable;

        var nullableSuffix = "";
        var nullablePrimitive = false;

        // Check of het een Nullable<T> is
        while (Nullable.GetUnderlyingType(type) != null)
        {
            var innerType = Nullable.GetUnderlyingType(type)!;
            if (innerType.IsGenericParameter)
            {
                nullablePrimitive = true;
                break;
            }
            else
            {
                nullableSuffix += "?";
                type = innerType;
            }
        }

        if (nullablePrimitive)
        {
            _Name = GetSimpleCsTypeByName(type.Name);
            IsDateTime = _Name == "DateTime";
            IsGuid = _Name == "Guid";
            IsCheckbox = _Name == "bool" || _Name == "bool?";
            IsNumber = _Name == "int" || _Name == "long" || _Name == "float" || _Name == "double";
            UnderlayingTypes = Array.Empty<TypeHelper>();
        }
        else
        {
            if (isNullable && nullableSuffix.Length == 0)
                nullableSuffix += "?";

            if (type.IsArray)
            {
                IsArray = true;
                _Name = "";
                _NameEnd = "[]" + nullableSuffix;
                UnderlayingTypes = [new TypeHelper(type.GetElementType()!)];
            }
            else
            {
                _Name = type.Name.Split('`').First();
                _Name = GetSimpleCsTypeByName(_Name);
                _NameEnd = nullableSuffix;
                Namespace = type.Namespace;

                if (type.IsGenericType)
                {
                    IsGenericType = true;
                    IsTaskT = type.Name.StartsWith("Task");
                    IsBaseResponseT = type.Name.StartsWith("BaseResponseT");
                    IsBaseListResponseT = type.Name.StartsWith("BaseListResponseT");
                    UnderlayingTypes = type.GetGenericArguments()
                        .Select(t => new TypeHelper(t))
                        .ToArray();
                }
                else
                {
                    IsString = type == typeof(string);
                    IsValueType = type.IsValueType;
                    IsBaseResponse = type.Name.StartsWith("BaseResponse");
                    IsEnum = type.IsEnum;
                    IsTask = type.Name.StartsWith("Task");
                    IsVoid = type.Name == "void";
                    IsDateTime = _Name == "DateTime";
                    IsCheckbox = _Name == "bool" || _Name == "bool?";
                    IsNumber = _Name == "int" || _Name == "long" || _Name == "float" || _Name == "double";
                    IsGenericType = false;
                    UnderlayingTypes = Array.Empty<TypeHelper>();
                }
            }
        }
    }

    public Type Type { get; }
    public bool IsNullable { get; }

    public string Name
    {
        get
        {
            if (IsArray)
                return $"{UnderlayingTypes[0].Name}{_NameEnd}";
            if (IsGenericType)
                return $"{_Name}<{string.Join(",", UnderlayingTypes.Select(a => a.Name))}>{_NameEnd}";
            return $"{_Name}{_NameEnd}";
        }
    }

    public string FullName
    {
        get
        {
            if (!IsGenericType)
                return _FullName;
            else
                return $"{_FullName}<{string.Join(",", UnderlayingTypes.Select(a => a.FullName))}>";
        }
    }

    public string[] Namespaces
    {
        get
        {
            if (Namespace == null)
            {
                return UnderlayingTypes.SelectMany(a => a.Namespaces).ToArray();
            }

            if (!IsGenericType)
                return new[] { Namespace };

            var list = UnderlayingTypes.SelectMany(a => a.Namespaces).ToList();
            list.Insert(0, Namespace);
            return list.ToArray();
        }
    }

    public string _Name { get; }
    public bool IsEnum { get; }
    public bool IsDateTime { get; }
    public bool IsGuid { get; }
    public bool IsCheckbox { get; }
    public bool IsArray { get; }
    public bool IsGenericType { get; }
    public bool IsValueType { get; }
    public bool IsNumber { get; }
    public bool IsVoid { get; }
    public bool IsTask { get; }
    public bool IsTaskT { get; }
    public bool IsBaseResponse { get; }
    public bool IsBaseResponseT { get; }
    public bool IsBaseListResponseT { get; }
    public bool IsString { get; }

    private readonly string? _NameEnd;

    public string? Namespace { get; }
    public string _FullName => $"{Namespace}.{_Name}";

    public TypeHelper[] UnderlayingTypes { get; }

    ITypeHelper[] ITypeHelper.UnderlayingTypes => UnderlayingTypes;

    public bool IsPrimitive => throw new NotImplementedException();

    public bool IsEntryPoint => throw new NotImplementedException();

    public bool IsJunction => throw new NotImplementedException();

    public bool IsUser => throw new NotImplementedException();

    public bool IsAuthorized => throw new NotImplementedException();

    public bool IsICrudEntity => throw new NotImplementedException();

    public ITypeHelper? JunctionLeftRealType => throw new NotImplementedException();

    public ITypeHelper? JunctionRightRealType => throw new NotImplementedException();

    public static string GetSimpleCsTypeByName(string name)
    {
        switch (name)
        {
            case "Int64":
                return "long";
            case "Int32":
                return "int";
            case "String":
                return "string";
            case "Double":
                return "double";
            case "Boolean":
                return "bool";
            case "Guid":
                return "Guid";
            case "DateTime":
                return "DateTime";
            case "Byte":
                return "byte";
            default:
                return name;
        }
    }

    public override bool Equals(object? obj) => obj is TypeHelper other && Equals(other);
    public bool Equals(TypeHelper other) => FullName == other.FullName;
    public static bool operator ==(TypeHelper left, TypeHelper right) => left.Equals(right);
    public static bool operator !=(TypeHelper left, TypeHelper right) => !left.Equals(right);

    public override int GetHashCode()
    {
        unchecked
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

    public ITypeHelperProperty[] GetProperties()
    {
        throw new NotImplementedException();
    }
}
