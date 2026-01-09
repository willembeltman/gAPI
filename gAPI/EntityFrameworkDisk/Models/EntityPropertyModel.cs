using gAPI.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace gAPI.EntityFrameworkDisk.Models;


public class EntityPropertyModel : IPropertyInfoRapport
{
    public EntityPropertyModel(EntityModel entity, PropertyInfo propertyInfo)
    {
        Entity = entity;
        PropertyInfo = propertyInfo;

        IsReadOnly = !propertyInfo.CanWrite;
        IsNotMapped = ReflectionHelper.HasNotMappedAttribute(propertyInfo);
        HasForeignKeyAttribute = ReflectionHelper.HasForeignKeyAttribute(propertyInfo);
        ForeignKeyAttributeName = HasForeignKeyAttribute ? ReflectionHelper.GetForeignKeyAttributeName(propertyInfo) : null;

        Rapport = new PropertyInfoRapport(propertyInfo);
    }

    public EntityModel Entity { get; }
    public PropertyInfo PropertyInfo { get; }
    public bool IsReadOnly { get; }
    public bool IsNotMapped { get; }
    public bool HasForeignKeyAttribute { get; }
    public string? ForeignKeyAttributeName { get; }
    public PropertyInfoRapport Rapport { get; }

    public bool IsForeignKey => ForeignKey != null;
    public bool IsNavigationItem => IsLijst == false && NavigationDbSet != null;
    public bool IsNavigationList => IsLijst == true && NavigationDbSet != null;

    public bool IsArrayType => ((IPropertyInfoRapport)Rapport).IsArrayType;
    public bool IsAsync => ((IPropertyInfoRapport)Rapport).IsAsync;
    public bool IsCheckbox => ((IPropertyInfoRapport)Rapport).IsCheckbox;
    public bool IsDateTime => ((IPropertyInfoRapport)Rapport).IsDateTime;
    public bool IsEnum => ((IPropertyInfoRapport)Rapport).IsEnum;
    public bool IsICollectionType => ((IPropertyInfoRapport)Rapport).IsICollectionType;
    public bool IsIEnumerableType => ((IPropertyInfoRapport)Rapport).IsIEnumerableType;
    public bool IsKey => ((IPropertyInfoRapport)Rapport).IsKey;
    public bool IsLijst => ((IPropertyInfoRapport)Rapport).IsLijst;
    public bool IsListType => ((IPropertyInfoRapport)Rapport).IsListType;
    public bool IsNullable => ((IPropertyInfoRapport)Rapport).IsNullable;
    public bool IsNumber => ((IPropertyInfoRapport)Rapport).IsNumber;
    public bool IsPrimitiveType => ((IPropertyInfoRapport)Rapport).IsPrimitiveType;
    public bool IsPrimitiveTypeOrEnumOrValueType => ((IPropertyInfoRapport)Rapport).IsPrimitiveTypeOrEnumOrValueType;
    public bool IsValueType => ((IPropertyInfoRapport)Rapport).IsValueType;
    public bool IsVirtual => ((IPropertyInfoRapport)Rapport).IsVirtual;
    public string Name => ((IPropertyInfoRapport)Rapport).Name;
    public Type Type => ((IPropertyInfoRapport)Rapport).Type;
    public string TypeSimpleName => ((IPropertyInfoRapport)Rapport).TypeSimpleName;
    public ValidationAttribute[] ValidationAttributes => ((IPropertyInfoRapport)Rapport).ValidationAttributes;

    public EntityPropertyModel? PrimaryKey
    {
        get
        {
            if (NavigationDbSet != null) return NavigationDbSet.Entity.PrimaryKey;
            if (ForeignKey?.NavigationDbSet != null) return ForeignKey.NavigationDbSet.Entity.PrimaryKey;
            return null;
        }
    }

    bool _NavigationDbSetLoaded;
    DbSetModel? _NavigationDbSet;
    public DbSetModel? NavigationDbSet
    {
        get
        {
            if (_NavigationDbSetLoaded == false)
            {
                _NavigationDbSetLoaded = true;
                _NavigationDbSet = Entity.DbSet.DbContext.DbSets
                    .FirstOrDefault(dbSet => dbSet.Type == Type);
            }
            return _NavigationDbSet;
        }
    }

    bool _ForeignKeyLoaded;
    EntityPropertyModel? _ForeignKey;
    public EntityPropertyModel? ForeignKey
    {
        get
        {
            if (NavigationList != null) return NavigationList;
            if (_ForeignKeyLoaded == false)
            {
                _ForeignKeyLoaded = true;
                _ForeignKey = Entity.Properties
                    .FirstOrDefault(a => a.NavigationItem == this);
            }
            return _ForeignKey;
        }
    }

    string? _NavigationItemName = null;
    private string? NavigationItemName
    {
        get
        {
            if (_NavigationItemName == null && IsNavigationItem)
            {
                _NavigationItemName =
                    HasForeignKeyAttribute
                    ? ForeignKeyAttributeName
                    : $"{Name}Id";
            }
            return _NavigationItemName;
        }
    }

    EntityPropertyModel? _NavigationItem = null;
    public EntityPropertyModel? NavigationItem
    {
        get
        {
            if (_NavigationItem == null && IsNavigationItem)
            {
                _NavigationItem = Entity.Properties
                    .FirstOrDefault(property => property.Name == NavigationItemName);
                if (_NavigationItem == null)
                    throw new Exception(
                        $"My framework is too stupid to figure out this foreign key please add a ForeignKeyAttribute " +
                        $"to property '{Name}' on entity '{Entity.Name}' with the correct foreign key name.");
            }
            return _NavigationItem;
        }
    }

    string? _NavigationListName = null;
    private string? NavigationListName
    {
        get
        {
            if (_NavigationListName == null && IsNavigationList)
            {
                _NavigationListName =
                    HasForeignKeyAttribute
                    ? ForeignKeyAttributeName
                    : $"{Entity.Name}Id";
            }
            return _NavigationListName;
        }
    }

    EntityPropertyModel? _NavigationList = null;
    public EntityPropertyModel? NavigationList
    {
        get
        {
            if (_NavigationList == null && IsNavigationList)
            {
                _NavigationList = NavigationDbSet?.Entity.Properties
                    .FirstOrDefault(property => property.Name == NavigationListName);
                if (_NavigationList == null)
                    throw new Exception(
                        $"My framework is too stupid to figure out this foreign key please add a ForeignKeyAttribute " +
                        $"to property '{Name}' on entity '{NavigationDbSet?.Entity.Name}' with the correct foreign key name.");
            }
            return _NavigationList;
        }
    }

    class First
    {
        [Key]
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        // PrimaryKey != null, wijst naar Second.Id
        // ForeignKey != null, wijst naar First.Second
        public long? SecondId { get; set; }

        // PrimaryKey != null, wijst naar Second.Id
        // NavigationDbSet != null, wijst naar Second
        // NavigationItem != null, wijst naar First.SecondId
        public virtual ILazy<Second>? Second { get; set; }
    }
    class Second
    {
        [Key]
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        // PrimaryKey != null, wijst naar Second.Id
        // NavigationDbSet != null, wijst naar First
        // NavigationList != null, wijst naar First.SecondId
        public virtual ICollection<First>? Firsts { get; set; }
    }
}