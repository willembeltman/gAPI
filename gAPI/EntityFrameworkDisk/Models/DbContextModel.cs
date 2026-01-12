using System;
using System.Linq;

namespace gAPI.EntityFrameworkDisk.Models;

public class DbContextModel
{
    public DbContextModel(Type type)
    {
        Type = type;
        FullName = type.FullName ?? type.Name;
        Name = type.Name;
        Namespace = type.Namespace!;
        DbSets = type.GetProperties()
            .Where(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => new DbSetModel(this, p))
            .ToArray();
    }

    public Type Type { get; }
    public string FullName { get; }
    public string Name { get; }
    public string Namespace { get; }
    public DbSetModel[] DbSets { get; }

    public DbSetModel? GetDbSet(Type type)
    {
        return DbSets.FirstOrDefault(a => a.Type == type);
    }
}