using System;
using System.Reflection;

namespace gAPI.EntityFrameworkDisk.Models;

public class DbSetModel
{
    public DbSetModel(DbContextModel db, PropertyInfo propertyInfo)
    {
        DbContext = db;
        PropertyInfo = propertyInfo;
        Type = propertyInfo.PropertyType.GenericTypeArguments[0];
        Name = propertyInfo.Name;
        Entity = new EntityModel(this, Type);
    }
    public DbContextModel DbContext { get; }
    public PropertyInfo PropertyInfo { get; }
    public Type Type { get; }
    public string Name { get; }
    public EntityModel Entity { get; }
}