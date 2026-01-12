using gAPI.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace gAPI.EntityFrameworkDisk.Models;

public class PropertyInfoRapport : IPropertyInfoRapport
{
    public PropertyInfoRapport(PropertyInfo propertyInfo)
    {
        PropertyInfo = propertyInfo;
        Name = propertyInfo.Name;

        IsKey = propertyInfo.GetCustomAttribute<KeyAttribute>() != null;

        IsVirtual = ReflectionHelper.IsVirtual(propertyInfo);
        IsAsync = ReflectionHelper.IsAsync(propertyInfo);

        Type = propertyInfo.PropertyType;
        if (IsAsync)
        {
            Type = Type.GenericTypeArguments[0];
        }

        IsNullable = ReflectionHelper.IsNullable(Type);

        IsIEnumerableType = ReflectionHelper.IsIEnumerable(Type);
        IsICollectionType = ReflectionHelper.IsICollection(Type);
        IsListType = ReflectionHelper.IsList(Type);
        IsArrayType = ReflectionHelper.IsArray(Type);

        if (IsArrayType)
        {
            Type = Type.GetElementType() ?? Type;
        }
        else if (IsLijst || IsNullable && Type.GenericTypeArguments.Length > 0)
        {
            Type = Type.GenericTypeArguments[0];
        }

        //if (Type == typeof(string))
        //{
        //    var context = new NullabilityInfoContext();
        //    var nullabilityInfo = context.Create(propertyInfo);
        //    IsNullable = nullabilityInfo.ReadState == NullabilityState.Nullable;
        //}

        IsPrimitiveType = ReflectionHelper.IsPrimitiveType(Type);
        IsEnum = Type.IsEnum;
        IsValueType = Type.IsValueType;
        IsPrimitiveTypeOrEnumOrValueType = ReflectionHelper.IsPrimitiveType(Type) || Type.IsEnum || Type.IsValueType;

        TypeSimpleName = PrimitiveTypesHelper.GetSimpleCsTypeByFullName(Type.FullName ?? Type.Name) ?? Type.FullName ?? Type.Name;
        if (IsEnum)
        {
            TypeSimpleName = Type.Name;
        }
        if (IsNullable)
        {
            TypeSimpleName += "?";
        }

        ValidationAttributes = propertyInfo
            .GetCustomAttributes<ValidationAttribute>(inherit: true)
            .ToArray();

        IsDateTime = Type == typeof(DateTime);
        IsCheckbox = Type == typeof(bool) || Type == typeof(bool?);
        IsNumber = Type == typeof(int) || Type == typeof(long) || Type == typeof(float) || Type == typeof(double);
    }

    public PropertyInfo PropertyInfo { get; }
    public string Name { get; }

    public Type Type { get; }
    public string TypeSimpleName { get; }

    public bool IsKey { get; }
    public bool IsVirtual { get; }
    public bool IsAsync { get; }
    public bool IsNullable { get; }
    public bool IsIEnumerableType { get; }
    public bool IsICollectionType { get; }
    public bool IsListType { get; }
    public bool IsArrayType { get; }
    public bool IsPrimitiveType { get; }
    public bool IsEnum { get; }
    public bool IsValueType { get; }
    public bool IsPrimitiveTypeOrEnumOrValueType { get; }

    public bool IsLijst => IsIEnumerableType || IsICollectionType || IsListType || IsArrayType;
    public ValidationAttribute[] ValidationAttributes { get; }
    public bool IsDateTime { get; }
    public bool IsCheckbox { get; }
    public bool IsNumber { get; }
}
