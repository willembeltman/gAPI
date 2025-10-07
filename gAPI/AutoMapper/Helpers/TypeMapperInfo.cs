using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoMapper.Helpers
{
    public class TypeMapperInfo
    {
        public TypeMapperInfo(Type topType)
        {
            TopType = topType;

            IsTopTypeNullable = topType.FullName.StartsWith("System.Nullable`");
            if (IsTopTypeNullable)
            {
                topType = topType.GenericTypeArguments.Single();
            }

            if (topType.IsArray)
            {
                IsArray = true;
                topType = topType.GetElementType();
            }
            else if (topType.IsGenericType)
            {
                if (topType.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    IsIEnumerable = true;
                }
                if (topType.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)))
                {
                    IsICollection = true;
                }
                IsList = topType.IsGenericType && topType.GetGenericTypeDefinition() == typeof(List<>);
                topType = topType.GenericTypeArguments.Single();
            }

            IsNullable = topType.FullName.StartsWith("System.Nullable`");
            if (IsNullable)
            {
                topType = topType.GenericTypeArguments.Single();
            }

            IsComplex =
                !topType.IsPrimitive &&
                topType != typeof(DateTime) &&
                topType != typeof(string);

            ElementType = topType;
        }

        public Type ElementType { get; }
        public Type TopType { get; }
        public bool IsTopTypeNullable { get; }

        public string FullName => ElementType.FullName;
        public bool IsEnum => ElementType.IsEnum;
        public bool IsNullable { get; }
        public bool IsArray { get; }
        public bool IsList { get; }
        public bool IsIEnumerable { get; }
        public bool IsICollection { get; }
        public bool IsComplex { get; }

        public static bool operator ==(TypeMapperInfo left, TypeMapperInfo right)
            => left.ElementType == right.ElementType
               && left.IsArray == right.IsArray
               && left.IsIEnumerable == right.IsIEnumerable;
        public static bool operator !=(TypeMapperInfo left, TypeMapperInfo right)
            => left.ElementType != right.ElementType
               || left.IsArray != right.IsArray
               || left.IsIEnumerable != right.IsIEnumerable;

        public static bool operator ==(TypeMapperInfo left, Type right)
            => left.ElementType == right;
        public static bool operator !=(TypeMapperInfo left, Type right)
            => left.ElementType != right;

        public override string ToString()
        {
            return $"{(IsICollection ? "ICollection<" : "")}{(IsIEnumerable ? "IEnumerable<" : "")}{(IsList ? "List<" : "")}{FullName}{(IsNullable ? "?" : "")}{(IsICollection || IsIEnumerable || IsList ? ">" : "")}{(IsArray ? "[]" : "")}{(IsTopTypeNullable ? "?" : "")}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is null || obj.GetType() != typeof(TypeMapperInfo))
                return false;
            var other = (TypeMapperInfo)obj;
            return FullName == other.FullName
                && IsEnum == other.IsEnum
                && IsArray == other.IsArray
                && IsList == other.IsList
                && IsNullable == other.IsNullable
                && IsIEnumerable == other.IsIEnumerable
                && IsICollection == other.IsICollection;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (FullName?.GetHashCode() ?? 0);
                hash = hash * 23 + IsEnum.GetHashCode();
                hash = hash * 23 + IsArray.GetHashCode();
                hash = hash * 23 + IsList.GetHashCode();
                hash = hash * 23 + IsNullable.GetHashCode();
                hash = hash * 23 + IsIEnumerable.GetHashCode();
                hash = hash * 23 + IsICollection.GetHashCode();
                return hash;
            }
        }
    }
}