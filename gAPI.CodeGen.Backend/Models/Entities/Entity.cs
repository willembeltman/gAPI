using gAPI.Attributes;
using gAPI.CodeGen.Backend.Helpers;
using System.Reflection;

namespace gAPI.CodeGen.Backend.Models.Entities;

public class Entity
{
    public Entity(DbSet dbSet, Type type)
    {
        DbSet = dbSet;
        Type = type;
        FullName = type.FullName ?? type.Name;
        Name = type.Name;

        IsStorageFile = ReflectionHelper.IsStorageFile(Type);

        IsHidden = type
            .GetCustomAttribute<IsHiddenAttribute>() != null;

        Properties = type
            .GetProperties()
            .Where(p => p.CanRead)
            .Select(p => new EntityProperty(this, p))
            .ToArray();

        IsAuthorize = type
            .GetCustomAttribute<IsAuthorizedAttribute>() != null;

        IsUser = type
            .GetCustomAttribute<IsUserAttribute>() != null;

        IsEntryPoint = type
            .GetCustomAttribute<IsEntryPointAttribute>() != null;
    }

    public DbSet DbSet { get; }
    public Type Type { get; }
    public string FullName { get; }
    public string Name { get; }
    public bool IsStorageFile { get; }
    public bool IsHidden { get; }
    public bool IsUser { get; }
    public bool IsEntryPoint { get; }
    public bool IsAuthorize { get; }
    public EntityProperty[] Properties { get; }

    bool? _IsJunctionTable;
    public bool IsJunctionTable
        => _IsJunctionTable ??=
            (Properties.All(a => a.IsForeignKey || a.IsNavigationItem || a.IsKey) &&
            Properties.Count(a => a.IsForeignKey) == 2);

    EntityProperty[]? _StateProperties;
    public EntityProperty[] StateProperties
        => _StateProperties ??= Properties
            .Where(a => a.ForeignKey != null)
            .ToArray();

    EntityProperty? _PrimaryKeyProperty;
    public EntityProperty KeyProperty
        => _PrimaryKeyProperty ??= Properties
            .First(a => a.IsKey);

    EntityProperty[]? _ForeignKeyProperties;
    public EntityProperty[] ForeignKeyProperties
        => _ForeignKeyProperties ??= Properties
            .Where(a => a.IsHidden == false && a.IsNavigationItem)
            .ToArray();

    EntityProperty[]? _ForeignListProperties;
    public EntityProperty[] ForeignListProperties
        => _ForeignListProperties ??= Properties
            .Where(a => a.IsHidden == false && a.IsNavigationList)
            .ToArray();
}