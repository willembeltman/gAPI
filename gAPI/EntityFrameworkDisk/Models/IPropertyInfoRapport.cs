using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace gAPI.EntityFrameworkDisk.Models;

public interface IPropertyInfoRapport
{
    bool IsArrayType { get; }
    bool IsAsync { get; }
    bool IsCheckbox { get; }
    bool IsDateTime { get; }
    bool IsEnum { get; }
    bool IsICollectionType { get; }
    bool IsIEnumerableType { get; }
    bool IsKey { get; }
    bool IsLijst { get; }
    bool IsListType { get; }
    bool IsNullable { get; }
    bool IsNumber { get; }
    bool IsPrimitiveType { get; }
    bool IsPrimitiveTypeOrEnumOrValueType { get; }
    bool IsValueType { get; }
    bool IsVirtual { get; }
    string Name { get; }
    PropertyInfo PropertyInfo { get; }
    Type Type { get; }
    string TypeSimpleName { get; }
    ValidationAttribute[] ValidationAttributes { get; }
}