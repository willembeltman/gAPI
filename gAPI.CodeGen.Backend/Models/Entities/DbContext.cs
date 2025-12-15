using Microsoft.EntityFrameworkCore;

namespace gAPI.CodeGen.Backend.Models.Entities;

public class DbContext
{
    public DbContext(Type type)
    {
        Type = type;
        FullName = type.FullName ?? type.Name;
        Name = type.Name;
        Namespace = type.Namespace;
        DbSets = type.GetProperties()
            .Where(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => new DbSet(this, p))
            .ToArray();

        var userdbcount = DbSets
            .Count(d => d.IsUser);
        if (userdbcount < 0 || 1 < userdbcount) throw new InvalidOperationException("No or too many User DbSet found.");

        var userentitycount = DbSets
            .Select(a => a.Entity)
            .Count(d => d.IsUser);
        if (userentitycount < 0 || 1 < userentitycount) throw new InvalidOperationException("No or too many User Entity found.");

        UserDbSet = DbSets
            .First(d => d.IsUser);
        UserEntity = DbSets
            .Select(a => a.Entity)
            .First(d => d.IsUser);
    }

    public Type Type { get; }
    public string FullName { get; }
    public string Name { get; }
    public string? Namespace { get; }
    public DbSet[] DbSets { get; }

    public DbSet UserDbSet { get; }
    public Entity UserEntity { get; }
}