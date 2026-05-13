using gAPI.CodeGen.Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace gAPI.CodeGen.Backend.Models;

public class DbContext : SharedReference
{
    public DbContext(Type type)
    {
        Type = type;
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

        var stateObjects = new List<StateObject>();
        StateUser = new StateObject(UserEntity, stateObjects);
        StateObjects = [.. stateObjects];
    }

    public Type Type { get; }
    public DbSet[] DbSets { get; }

    public DbSet UserDbSet { get; }
    public Entity UserEntity { get; }

    public StateObject StateUser { get; }
    public StateObject[] StateObjects { get; }
}