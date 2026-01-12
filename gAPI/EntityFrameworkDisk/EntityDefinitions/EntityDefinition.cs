using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace gAPI.EntityFrameworkDisk.EntityDefinitions;

public class EntityDefinition
{
    internal EntityDefinition(Type entityType)
    {
        KeyProperty = entityType
            .GetProperties()
            .First(p => p.GetCustomAttributes(typeof(KeyAttribute), true).Any());
    }
    public PropertyInfo KeyProperty { get; }

    public object? GetKeyValue(object? item)
        => item == null ? null : KeyProperty?.GetValue(item);
    public void SetKeyValue(object item, object key)
        => KeyProperty?.SetValue(item, key);

    public Type? KeyType
        => KeyProperty?.PropertyType;
    public string? KeyName
        => KeyProperty?.Name;

}
