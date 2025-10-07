//using System;
//using System.Linq;

//namespace gAPI.Helpers
//{
//    public class TypeHelper
//    {
//        public TypeHelper(Type type, bool isNullable = false)
//        {
//            Type = type ?? throw new ArgumentNullException(nameof(type));
//            IsNullable = isNullable;

//            var nullableSuffix = "";
//            var nullablePrimitive = false;

//            // Check of het een Nullable<T> is
//            while (Nullable.GetUnderlyingType(type) != null)
//            {
//                var inner = Nullable.GetUnderlyingType(type)!;
//                if (inner.IsGenericParameter)
//                {
//                    nullablePrimitive = true;
//                    break;
//                }
//                else
//                {
//                    nullableSuffix += "?";
//                    type = inner;
//                }
//            }

//            if (nullablePrimitive)
//            {
//                _Name = GetSimpleCsTypeByName(type.Name);
//                IsDateTime = _Name == "DateTime";
//                IsGuid = _Name == "Guid";
//                IsCheckbox = _Name == "bool" || _Name == "bool?";
//                IsNumber = _Name == "int" || _Name == "long" || _Name == "float" || _Name == "double";
//                UnderlayingTypes = Array.Empty<TypeHelper>();
//            }
//            else
//            {
//                if (isNullable && nullableSuffix.Length == 0)
//                    nullableSuffix += "?";

//                if (type.IsArray)
//                {
//                    IsArray = true;
//                    _Name = "";
//                    _NameEnd = "[]" + nullableSuffix;
//                    UnderlayingTypes = [new TypeHelper(type.GetElementType()!)];
//                }
//                else
//                {
//                    _Name = type.Name.Split('`').First();
//                    _Name = GetSimpleCsTypeByName(_Name);
//                    _NameEnd = nullableSuffix;
//                    Namespace = type.Namespace;

//                    if (type.IsGenericType)
//                    {
//                        IsGenericType = true;
//                        UnderlayingTypes = type.GetGenericArguments()
//                            .Select(t => new TypeHelper(t))
//                            .ToArray();
//                    }
//                    else
//                    {
//                        IsEnum = type.IsEnum;
//                        IsDateTime = _Name == "DateTime";
//                        IsCheckbox = _Name == "bool" || _Name == "bool?";
//                        IsNumber = _Name == "int" || _Name == "long" || _Name == "float" || _Name == "double";
//                        IsGenericType = false;
//                        UnderlayingTypes = Array.Empty<TypeHelper>();
//                    }
//                }
//            }
//        }

//        public Type Type { get; }
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
//                if (Namespace == null)
//                {
//                    return UnderlayingTypes.SelectMany(a => a.Namespaces).ToArray();
//                }

//                if (!IsGenericType)
//                    return new[] { Namespace };

//                var list = UnderlayingTypes.SelectMany(a => a.Namespaces).ToList();
//                list.Insert(0, Namespace);
//                return list.ToArray();
//            }
//        }

//        public string _Name { get; }
//        public bool IsEnum { get; }
//        public bool IsDateTime { get; }
//        public bool IsGuid { get; }
//        public bool IsCheckbox { get; }
//        public bool IsArray { get; }

//        private readonly string? _NameEnd;

//        public string? Namespace { get; }
//        public string _FullName => $"{Namespace}.{_Name}";
//        public bool IsGenericType { get; }

//        public TypeHelper[] UnderlayingTypes { get; }
//        public bool IsNumber { get; set; }

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

//        public override bool Equals(object obj) => obj is TypeHelper other && Equals(other);
//        public bool Equals(TypeHelper other) => FullName == other.FullName;
//        public static bool operator ==(TypeHelper left, TypeHelper right) => left.Equals(right);
//        public static bool operator !=(TypeHelper left, TypeHelper right) => !left.Equals(right);

//        public override int GetHashCode()
//        {
//            unchecked
//            {
//                int hash = 17;
//                hash = hash * 23 + (FullName?.GetHashCode() ?? 0);
//                hash = hash * 23 + IsNullable.GetHashCode();
//                return hash;
//            }
//        }

//        public override string ToString()
//        {
//            return FullName;
//        }
//    }
//}
