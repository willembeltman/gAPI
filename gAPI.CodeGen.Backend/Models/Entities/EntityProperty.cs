using gAPI.Attributes;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace gAPI.CodeGen.Backend.Models.Entities;

public class EntityProperty : IPropertyInfoRapport, INameProperty
{
    public EntityProperty(Entity entity, PropertyInfo propertyInfo)
    {
        Entity = entity;
        PropertyInfo = propertyInfo;

        IsReadOnly = !propertyInfo.CanWrite;
        IsHidden = propertyInfo
            .GetCustomAttribute<IsHiddenAttribute>() != null;
        IsState = propertyInfo
            .GetCustomAttribute<IsStateAttribute>() != null;
        IsUnique = propertyInfo
            .GetCustomAttribute<IsUniqueAttribute>() != null;

        IsNotMapped = ReflectionHelper.HasNotMappedAttribute(propertyInfo);

        HasForeignKeyAttribute = ReflectionHelper.HasForeignKeyAttribute(propertyInfo);
        ForeignKeyAttributeName = HasForeignKeyAttribute ? ReflectionHelper.GetForeignKeyAttributeName(propertyInfo) : null;

        Rapport = new PropertyInfoRapport(propertyInfo);
    }

    public Entity Entity { get; }
    public PropertyInfo PropertyInfo { get; }
    public bool IsHidden { get; }
    public bool IsState { get; }
    public bool IsUnique { get; }
    public bool IsReadOnly { get; }
    public bool IsNotMapped { get; }
    public bool HasForeignKeyAttribute { get; }
    public string? ForeignKeyAttributeName { get; }
    public PropertyInfoRapport Rapport { get; }

    public bool IsStateManaged => StateUserProperty != null;
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

    public IsNameAttribute IsName => ((IPropertyInfoRapport)Rapport).IsName;

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


    bool _NavigationDbSetLoaded;
    DbSet? _NavigationDbSet;
    public DbSet? NavigationDbSet
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
    EntityProperty? _ForeignKey;
    public EntityProperty? ForeignKey
    {
        get
        {
            if (_ForeignKeyLoaded == false)
            {
                _ForeignKeyLoaded = true;
                _ForeignKey = Entity.Properties
                    .FirstOrDefault(a => a.NavigationItemProperty == this);
            }
            return _ForeignKey;
        }
    }

    string? _NavigationItemPropertyName = null;
    private string? NavigationItemPropertyName
    {
        get
        {
            if (_NavigationItemPropertyName == null && IsNavigationItem)
            {
                _NavigationItemPropertyName =
                    HasForeignKeyAttribute
                    ? ForeignKeyAttributeName
                    : $"{Name}Id";
            }
            return _NavigationItemPropertyName;
        }
    }
    EntityProperty? _NavigationItemProperty = null;
    public EntityProperty? NavigationItemProperty
    {
        get
        {
            if (_NavigationItemProperty == null && IsNavigationItem)
            {
                _NavigationItemProperty = Entity.Properties
                    .FirstOrDefault(property => property.Name == NavigationItemPropertyName);
                if (_NavigationItemProperty == null)
                    throw new Exception(
                        $"My framework is too stupid to figure out this foreign key please add a ForeignKeyAttribute " +
                        $"to property '{Name}' on entity '{Entity.Name}' with the correct foreign key name.");
            }
            return _NavigationItemProperty;
        }
    }

    string? _NavigationListPropertyName = null;
    private string? NavigationListPropertyName
    {
        get
        {
            if (_NavigationListPropertyName == null && IsNavigationList)
            {
                _NavigationListPropertyName =
                    HasForeignKeyAttribute
                    ? ForeignKeyAttributeName
                    : $"{Entity.Name}Id";
            }
            return _NavigationListPropertyName;
        }
    }
    EntityProperty? _NavigationListProperty = null;
    public EntityProperty? NavigationListProperty
    {
        get
        {
            if (_NavigationListProperty == null && IsNavigationList && !NavigationDbSet!.Entity.IsHidden)
            {
                _NavigationListProperty = NavigationDbSet!.Entity.Properties
                    .FirstOrDefault(property => property.Name == NavigationListPropertyName);
                if (_NavigationListProperty == null)
                    throw new Exception(
                        $"My framework is too stupid to figure out this foreign key please add a ForeignKeyAttribute " +
                        $"to property '{Name}' on entity '{NavigationDbSet!.Entity.Name}' with the correct foreign key name.");
            }
            return _NavigationListProperty;
        }
    }

    public EntityProperty? StateUserProperty
    {
        get
        {
            if (ForeignKey == null) return null;
            if (ForeignKey.NavigationDbSet == null) return null;
            if (Entity.DbSet.DbContext.UserEntity.Properties
                .Any(a =>
                    a.ForeignKey != null &&
                    a.ForeignKey.NavigationDbSet != null &&
                    a.ForeignKey.NavigationDbSet.Entity == Entity))
                return null;

            var item = Entity.DbSet.DbContext.UserEntity.StateProperties
                .FirstOrDefault(a =>
                    a.ForeignKey != null &&
                    a.ForeignKey.NavigationDbSet != null &&
                    a.ForeignKey.NavigationDbSet.Entity == ForeignKey.NavigationDbSet.Entity);

            return item;
        }
    }

}