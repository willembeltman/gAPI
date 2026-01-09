using gAPI.AutoApiClient.Contexts;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoApiClient.Helpers;

internal class TypeHelper
{
    public TypeHelper(ServiceContext dataModel, ITypeSymbol typeSymbol, bool isNullable = false) // Hier komt hij dus binnen met long?, wat eigenlijk Nullable<T> is, met daarin een long.
    {
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
            _Name = typeSymbol.ToDisplayString();

            IsDateTime = _Name == "DateTime";
            IsCheckbox = _Name == "bool" || _Name == "bool?";
            IsNumber = _Name == "int" || _Name == "long" || _Name == "float" || _Name == "double";

            UnderlayingTypes = new TypeHelper[0];
        }
        else
        {
            if (isNullable && nullable.Length == 0)
                nullable += "?";

            if (typeSymbol.TypeKind == TypeKind.Array && typeSymbol is IArrayTypeSymbol array)
            {
                IsArray = true;
                _Name = "";
                _NameEnd = "[]" + nullable;
                UnderlayingTypes = new TypeHelper[] { new TypeHelper(dataModel, array.ElementType) };
            }
            else
            {
                _Name = typeSymbol.Name.Split('`').First();
                _Name = GetSimpleCsTypeByName(_Name);
                _NameEnd = nullable;
                Namespace = typeSymbol.ContainingNamespace?.ToDisplayString();

                if (typeSymbol is INamedTypeSymbol namedType)
                {
                    IsGenericType = namedType.IsGenericType;
                    UnderlayingTypes = namedType.TypeArguments
                        .Select(t => new TypeHelper(dataModel, t))
                        .ToArray();
                }
                else
                {
                    IsEnum = typeSymbol.TypeKind == TypeKind.Enum;
                    IsDateTime = _Name == "DateTime";
                    IsCheckbox = _Name == "bool" || _Name == "bool?";
                    IsNumber = _Name == "int" || _Name == "long" || _Name == "float" || _Name == "double";

                    IsGenericType = false;
                    UnderlayingTypes = new TypeHelper[0];
                }
            }
        }
    }

    public ITypeSymbol TypeSymbol { get; }
    public ServiceContext DataModel { get; }
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
    public bool IsCheckbox { get; }
    public bool IsArray { get; }

    private readonly string _NameEnd;

    public string Namespace { get; }
    public string _FullName => $"{Namespace}.{_Name}";
    public bool IsGenericType { get; }

    public TypeHelper[] UnderlayingTypes { get; }
    public bool IsNumber { get; internal set; }

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


    public override bool Equals(object obj) => obj is TypeHelper other && Equals(other);
    public bool Equals(TypeHelper other) => FullName == other.FullName;
    public static bool operator ==(TypeHelper left, TypeHelper right) => left.Equals(right);
    public static bool operator !=(TypeHelper left, TypeHelper right) => !left.Equals(right);

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

}



//using Microsoft.CodeAnalysis;
//using System.Linq;

//namespace gAPI.AutoApi.Models
//{
//    public class TypeHelper
//    {
//        public TypeHelper(ITypeSymbol type, bool isNull = false)
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

//        public ITypeSymbol Type { get; }
//        public bool IsNullable { get; }

//        public string Name
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
//        public string FullName
//        {
//            get
//            {
//                if (!IsGenericType)
//                    return _FullName;
//                else
//                    return $"{_FullName}<{string.Join(",", UnderlayingTypes.Select(a => a.FullName))}>";
//            }
//        }
//        public string[] Namespaces
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

//        public string _Name { get; }
//        public bool IsArray { get; }

//        private string _NameEnd;

//        public string _Namespace { get; }
//        public string _FullName => $"{_Namespace}.{_Name}";
//        public bool IsGenericType { get; }

//        public TypeHelper[] UnderlayingTypes { get; }

//        public static string GetSimpleCsTypeByName(string name)
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