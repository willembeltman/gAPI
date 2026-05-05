using gAPI.Core.Server.Storage;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace gAPI.CodeGen.Backend.Helpers;

public static class ReflectionHelper
{

    private static readonly HashSet<Type> PrimitiveTypes =
    [
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(char),
        typeof(decimal),
        typeof(double),
        typeof(float),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong)
    ];



    public static bool IsICollection(PropertyInfo prop) => IsICollection(prop.PropertyType);
    public static bool IsICollection(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(ICollection<>);
    }

    public static bool IsIEnumerable(PropertyInfo prop) => IsIEnumerable(prop.PropertyType);
    public static bool IsIEnumerable(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    }

    public static bool IsList(PropertyInfo prop) => IsList(prop.PropertyType);
    public static bool IsList(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(List<>);
    }

    public static bool IsLazy(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Lazy<>);
    }
    public static bool IsVirtual(PropertyInfo prop)
    {
        var method = prop.GetGetMethod(true);
        if (method == null)
            return false;
        return method.IsVirtual && !method.IsFinal;
    }
    //private static bool HasAnyProperties(Type propType)
    //{
    //    return propType.GetProperties().Length != 0;
    //}

    public static bool HasPublicGetter(PropertyInfo prop)
    {
        var getter = prop.GetGetMethod(false);
        return getter != null;
    }
    public static bool HasPublicSetter(PropertyInfo prop)
    {
        return prop.GetSetMethod(false) != null;
    }
    public static bool HasNotMappedAttribute(PropertyInfo prop)
    {
        return
            prop.GetCustomAttribute<NotMappedAttribute>() != null ||
            prop.GetCustomAttribute<NotMappedAttribute>() != null;
    }
    public static bool HasForeignKeyAttribute(PropertyInfo prop)
    {
        return
            prop.GetCustomAttribute<ForeignKeyAttribute>() != null;
    }
    public static string? GetForeignKeyAttributeName(PropertyInfo prop)
    {
        var attr1 = prop.GetCustomAttribute<ForeignKeyAttribute>();
        return attr1?.Name;
    }
    public static Type GetDbSetType(PropertyInfo prop)
    {
        return prop.PropertyType.GenericTypeArguments[0];
    }
    public static Type GetDbSetType(Type dbSetType)
    {
        return dbSetType.GenericTypeArguments[0];
    }
    public static Type GetIEnumerableType(PropertyInfo prop)
    {
        return prop.PropertyType.GenericTypeArguments[0];
    }
    public static bool IsStorageFileUrlProperty(Type type)
    {
        // Checks if the type or any of its interfaces is named "IStorageFile"
        return type.GetInterfaces().Any(i => i.Name == nameof(IStorageFile));
    }
    public static Type GetILazyType(PropertyInfo prop)
    {
        return prop.PropertyType.GenericTypeArguments[0];
    }
    public static bool IsArray(Type type)
    {
        return type.IsArray;
    }

    public static bool IsAsync(PropertyInfo prop) => IsAsync(prop.PropertyType);
    public static bool IsAsync(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Task<>);
    }
    public static bool IsNullable(PropertyInfo prop)
    {
        if (prop.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(prop.PropertyType) != null;
        }
        else
        {
            var context = new NullabilityInfoContext();
            var nullabilityInfo = context.Create(prop);
            return nullabilityInfo.ReadState == NullabilityState.Nullable;
        }
    }
    public static bool IsNullable(Type type)
    {
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }
        else
        {
            return true; // Referentietypen zijn altijd nullable
        }
    }
    public static bool IsNullable(ParameterInfo prop)
    {
        if (prop.ParameterType.IsValueType)
        {
            return Nullable.GetUnderlyingType(prop.ParameterType) != null;
        }
        else
        {
            var context = new NullabilityInfoContext();
            var nullabilityInfo = context.Create(prop);
            return nullabilityInfo.ReadState == NullabilityState.Nullable;
        }
    }
    //public static bool IsDbSet(PropertyInfo prop)
    //{
    //    if (!prop.PropertyType.IsGenericType)
    //        return false;

    //    var typeDef = prop.PropertyType.GetGenericTypeDefinition();
    //    return typeDef == typeof(DbSet<>);
    //}
    //public static bool HasNavigationProperties(Type type, HashSet<Type>? visitedTypes = null)
    //{
    //    visitedTypes ??= [];
    //    if (visitedTypes.Contains(type))
    //        return true; // Avoid cycles

    //    visitedTypes.Add(type);

    //    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    //    {
    //        if (!HasPublicGetter(prop)) continue;
    //        if (!HasPublicSetter(prop)) continue;

    //        if (IsNavigationEntityProperty(prop)) return true;
    //        if (IsNavigationCollectionProperty(prop)) return true;
    //    }

    //    return false;
    //}
    //public static bool IsNavigationProperty(PropertyInfo prop)
    //{
    //    return IsNavigationCollectionProperty(prop) || IsNavigationEntityProperty(prop);
    //}
    //public static bool IsNavigationEntityProperty(PropertyInfo prop)
    //{
    //    return
    //        IsVirtual(prop) &&
    //        IsILazy(prop);
    //}
    //public static bool IsOldNavigationEntityProperty(PropertyInfo prop)
    //{
    //    return
    //        IsVirtual(prop) &&
    //        IsLazy(prop);
    //}
    //public static bool IsNavigationCollectionProperty(PropertyInfo prop)
    //{
    //    return
    //        IsVirtual(prop) &&
    //        (IsIEnumerable(prop) || IsICollection(prop));
    //}
    //public static bool IsValidChildEntity(Type type, HashSet<Type>? visitedTypes = null)
    //{
    //    visitedTypes ??= [];
    //    if (visitedTypes.Contains(type))
    //        return true; // Avoid cycles
    //    visitedTypes.Add(type);

    //    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    //    {
    //        if (!HasPublicGetter(prop)) continue;
    //        if (!HasPublicSetter(prop)) continue;

    //        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

    //        if (HasNotMappedAttribute(prop)) continue;
    //        if (IsNavigationEntityProperty(prop)) continue;
    //        if (IsNavigationCollectionProperty(prop)) continue;

    //        if (IsPrimitiveTypeOrEnum(propType)) continue;

    //        if (IsGenericType(propType)) return false;
    //        if (!HasAnyProperties(propType)) return false;

    //        if (propType.IsClass || propType.IsValueType)
    //        {
    //            if (!IsValidChildEntity(propType, visitedTypes))
    //                return false;
    //        }
    //        else
    //        {
    //            return false;
    //        }

    //        return true;
    //    }

    //    return true;
    //}
    public static bool IsGenericType(Type propType)
    {
        return propType.IsGenericType;
    }
    public static bool IsPrimitiveTypeOrEnum(Type propertyType)
    {
        return IsPrimitiveType(propertyType) || propertyType.IsEnum;
    }
    public static bool IsPrimitiveType(Type propertyType)
    {
        return PrimitiveTypes.Contains(propertyType);
    }

    //public static bool IsIEntity(Type type)
    //{
    //    if (type == typeof(string)) return false;

    //    return type.GetInterfaces()
    //        .Any(i => i == typeof(IEntity));
    //}
    public static Type GetUnderlyingType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    public static string GetNamespace(Type type)
    {
        return type.Namespace ?? string.Empty;
    }

}

