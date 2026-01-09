using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoHub.Helpers;

internal class TypeHelper
{
    public TypeHelper(ServiceContext dataModel, Type type) { }
    public TypeHelper(ServiceContext dataModel, ITypeSymbol type, bool isNullable = false) // Hier komt hij dus binnen met long?, wat eigenlijk Nullable<T> is, met daarin een long.
    {
        Type = type;
        DataModel = dataModel;
        IsNullable = isNullable;

        var nullable = "";
        var nulleblePrimitive = false;
        // Check of het een Nullable<T> is
        while (type.OriginalDefinition is INamedTypeSymbol named &&
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
                type = named.TypeArguments[0];
            }
        }

        if (nulleblePrimitive)
        {
            _Name = type.ToDisplayString();
            UnderlayingTypes = new TypeHelper[0];
        }
        else
        {
            if (isNullable && nullable.Length == 0)
                nullable += "?";

            if (type.TypeKind == TypeKind.Array && type is IArrayTypeSymbol array)
            {
                IsArray = true;
                _Name = "";
                _NameEnd = "[]" + nullable;
                UnderlayingTypes = new TypeHelper[] { new TypeHelper(dataModel, array.ElementType) };
            }
            else
            {
                _Name = type.Name.Split('`').First();
                _Name = GetSimpleCsTypeByName(_Name);
                _NameEnd = nullable;
                _Namespace = type.ContainingNamespace?.ToDisplayString();

                if (type is INamedTypeSymbol namedType)
                {
                    IsGenericType = namedType.IsGenericType;
                    UnderlayingTypes = namedType.TypeArguments
                        .Select(t => new TypeHelper(dataModel, t))
                        .ToArray();
                }
                else
                {
                    IsGenericType = false;
                    UnderlayingTypes = new TypeHelper[0];
                }
            }
        }
    }

    internal ITypeSymbol Type { get; }
    public ServiceContext DataModel { get; }
    public bool IsNullable { get; }

    internal string Name
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

    internal string FullName
    {
        get
        {
            if (!IsGenericType)
                return _FullName;
            else
                return $"{_FullName}<{string.Join(",", UnderlayingTypes.Select(a => a.FullName))}>";
        }
    }

    internal string[] Namespaces
    {
        get
        {
            if (_Namespace == null)
            {
                return UnderlayingTypes.SelectMany(a => a.Namespaces).ToArray();
            }

            if (!IsGenericType)
                return new[] { _Namespace };

            var list = UnderlayingTypes.SelectMany(a => a.Namespaces).ToList();
            list.Insert(0, _Namespace);
            return list.ToArray();
        }
    }

    internal string _Name { get; }
    internal bool IsArray { get; }

    private readonly string _NameEnd;

    internal string _Namespace { get; }
    internal string _FullName => $"{_Namespace}.{_Name}";
    internal bool IsGenericType { get; }

    internal TypeHelper[] UnderlayingTypes { get; }

    internal static string GetSimpleCsTypeByName(string name)
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
}



//using Microsoft.CodeAnalysis;
//using System.Linq;

//namespace gAPI.AutoHub.Models
//{
//    internal class TypeHelper
//    {
//        internal TypeHelper(ITypeSymbol type, bool isNull = false)
//        {
//            Type = type;
//            IsNullable = isNull;

//            var nullable = "";
//            while (type.Name.StartsWith("Nullable`") && type.ContainingNamespace.ToDisplayString() == "System")
//            {
//                nullable += "?";
//                type = type.GenericTypeArguments[0]; // GenericTypeArguments bestaat niet op type.
//            }

//            if (isNull == true && nullable.Length == 0)
//                nullable += "?";

//            if (type.IsArray)
//            {
//                IsArray = true;
//                _Name = "";
//                _NameEnd = "[]" + nullable;
//                UnderlayingTypes = new TypeHelper[] { new TypeHelper(type.GetElementType()) }; // GetElementType bestaat niet op type
//            }
//            else
//            {
//                _Name = type.Name.Split(new char[] { '`' }).First();
//                _Name = GetSimpleCsTypeByName(_Name);
//                _NameEnd = nullable;
//                _Namespace = type.ContainingNamespace.ToDisplayString();

//                IsGenericType = type.IsGenericType; // IsGenericType bestaat niet op type
//                UnderlayingTypes = type.GenericTypeArguments.Select(a => new TypeHelper(a)).ToArray(); // GenericTypeArguments bestaat niet op type
//            }
//        }

//        internal ITypeSymbol Type { get; }
//        internal bool IsNullable { get; }

//        internal string Name
//        {
//            get
//            {
//                if (IsArray)
//                    return $"{UnderlayingTypes[0].Name}{_NameEnd}";
//                if (IsGenericType)
//                    return $"{_Name}<{string.Join(",", UnderlayingTypes.Select(a => a.Name))}>{_NameEnd}";
//                return $"{_Name}{_NameEnd}";
//            }
//        }
//        internal string FullName
//        {
//            get
//            {
//                if (!IsGenericType)
//                    return _FullName;
//                else
//                    return $"{_FullName}<{string.Join(",", UnderlayingTypes.Select(a => a.FullName))}>";
//            }
//        }
//        internal string[] Namespaces
//        {
//            get
//            {
//                if (_Namespace == null)
//                {
//                    if (!IsGenericType)
//                        return new string[0];
//                    else
//                        return UnderlayingTypes.SelectMany(a => a.Namespaces).ToArray();
//                }
//                else
//                {
//                    if (!IsGenericType)
//                        return new string[] { _Namespace };
//                    else
//                    {
//                        var list = UnderlayingTypes.SelectMany(a => a.Namespaces).ToList();
//                        list.Insert(0, _Namespace);
//                        return list.ToArray();
//                    }
//                }
//            }
//        }

//        internal string _Name { get; }
//        internal bool IsArray { get; }

//        private string _NameEnd;

//        internal string _Namespace { get; }
//        internal string _FullName => $"{_Namespace}.{_Name}";
//        internal bool IsGenericType { get; }

//        internal TypeHelper[] UnderlayingTypes { get; }

//        internal static string GetSimpleCsTypeByName(string name)
//        {
//            switch (name)
//            {
//                case "Int64":
//                    return "long";
//                case "Int32":
//                    return "int";
//                case "String":
//                    return "string";
//                case "Double":
//                    return "double";
//                case "Boolean":
//                    return "bool";
//                case "Guid":
//                    return "Guid";
//                case "DateTime":
//                    return "DateTime";
//                case "Byte":
//                    return "byte";
//                default:
//                    return name;
//            }
//        }
//    }
//}