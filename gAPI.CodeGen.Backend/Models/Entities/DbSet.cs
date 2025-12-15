using System.Reflection;

namespace gAPI.CodeGen.Backend.Models.Entities;

public class DbSet
{
    public DbSet(DbContext db, PropertyInfo propertyInfo)
    {
        DbContext = db;
        PropertyInfo = propertyInfo;
        Type = propertyInfo.PropertyType.GenericTypeArguments[0];
        Name = propertyInfo.Name;
        Entity = new Entity(this, Type);
    }
    public DbContext DbContext { get; }
    public PropertyInfo PropertyInfo { get; }
    public Type Type { get; }
    public string Name { get; }
    public Entity Entity { get; }

    public bool IsUser => Entity.IsUser;
}