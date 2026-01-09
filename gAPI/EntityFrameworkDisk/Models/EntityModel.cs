using System;
using System.Linq;

namespace gAPI.EntityFrameworkDisk.Models;


public class EntityModel
{
    public EntityModel(DbSetModel dbSet, Type type)
    {
        DbSet = dbSet;
        Type = type;
        FullName = type.FullName ?? type.Name;
        Name = type.Name;

        Properties = type
            .GetProperties()
            .Where(p => p.CanRead)
            .Select(p => new EntityPropertyModel(this, p))
            .ToArray();
    }

    public DbSetModel DbSet { get; }
    public Type Type { get; }
    public string FullName { get; }
    public string Name { get; }
    public EntityPropertyModel[] Properties { get; }

    public EntityPropertyModel? PrimaryKey => Properties?.FirstOrDefault(a => a.IsKey);

    bool? _IsJunctionTable;
    public bool IsJunctionTable
    {
        get
        {
            _IsJunctionTable =
                _IsJunctionTable ??
                (Properties.All(a => a.IsForeignKey || a.IsNavigationItem || a.IsKey) &&
                Properties.Count(a => a.IsForeignKey) == 2);
            return _IsJunctionTable.Value;
        }
    }
    EntityPropertyModel[]? _StateProperties;
    public EntityPropertyModel[] StateProperties
    {
        get
        {
            _StateProperties = _StateProperties ?? Properties
            .Where(a => a.ForeignKey != null)
            .ToArray();
            return _StateProperties;
        }
    }

    EntityPropertyModel? _PrimaryKeyProperty;
    public EntityPropertyModel KeyProperty
    {
        get
        {
            _PrimaryKeyProperty = _PrimaryKeyProperty ?? Properties
                    .First(a => a.IsKey);
            return _PrimaryKeyProperty;
        }
    }

    EntityPropertyModel[]? _ForeignKeyProperties;
    public EntityPropertyModel[] ForeignKeyProperties
    {
        get
        {
            _ForeignKeyProperties = _ForeignKeyProperties ?? Properties
                   .Where(a => a.IsNavigationItem)
                   .ToArray();
            return _ForeignKeyProperties;
        }
    }

    EntityPropertyModel[]? _ForeignListProperties;
    public EntityPropertyModel[] ForeignListProperties
    {
        get
        {
            _ForeignListProperties = _ForeignListProperties ?? Properties
                    .Where(a => a.IsNavigationList)
                    .ToArray();
            return _ForeignListProperties;
        }
    }
}