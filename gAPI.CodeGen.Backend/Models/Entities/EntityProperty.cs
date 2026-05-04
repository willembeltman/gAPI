using gAPI.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace gAPI.CodeGen.Backend.Models.Entities;

public class EntityProperty : IPropertyInfoRapport
{
    public EntityProperty(Entity entity, PropertyInfo propertyInfo)
    {
        ParentEntity = entity;
        PropertyInfo = propertyInfo;

        IsReadOnly = !propertyInfo.CanWrite || propertyInfo
            .GetCustomAttribute<IsReadOnlyAttribute>() != null;
        IsImmutable = propertyInfo
            .GetCustomAttribute<IsImmutableAttribute>() != null;
        IsHidden = propertyInfo
            .GetCustomAttribute<IsHiddenAttribute>() != null;
        IsState = propertyInfo
            .GetCustomAttribute<IsStateAttribute>() != null;
        IsNotMapped = propertyInfo
            .GetCustomAttribute<NotMappedAttribute>() != null;
        IsStateManaged = propertyInfo
            .GetCustomAttribute<IsStateManagedAttribute>();
        ForeignKeyAttribute = propertyInfo
            .GetCustomAttribute<ForeignKeyAttribute>();

        Rapport = new PropertyInfoRapport(propertyInfo);
    }

    public Entity ParentEntity { get; }
    public PropertyInfo PropertyInfo { get; }
    public bool IsHidden { get; }
    public bool IsState { get; }
    public bool IsReadOnly { get; }
    public bool IsNotMapped { get; }
    public IsStateManagedAttribute? IsStateManaged { get; }
    public ForeignKeyAttribute? ForeignKeyAttribute { get; }
    public PropertyInfoRapport Rapport { get; }

    public bool IsForeignKey => ForeignKeyProperty != null || ForeignKeyAttribute != null;
    public bool IsNavigationItem => IsLijst == false && NavigationDbSet != null;
    public bool IsNavigationList => IsLijst == true && NavigationDbSet != null;

    #region Rapport
    public bool IsArrayType => Rapport.IsArrayType;
    public bool IsAsync => Rapport.IsAsync;
    public bool IsCheckbox => Rapport.IsCheckbox;
    public bool IsDateTime => Rapport.IsDateTime;
    public bool IsEnum => Rapport.IsEnum;
    public bool IsICollectionType => Rapport.IsICollectionType;
    public bool IsIEnumerableType => Rapport.IsIEnumerableType;
    public bool IsKey => Rapport.IsKey;
    public bool IsLijst => Rapport.IsLijst;
    public bool IsListType => Rapport.IsListType;
    public IsNameAttribute IsName => Rapport.IsName;
    public bool IsNullable => Rapport.IsNullable;
    public bool IsNumber => Rapport.IsNumber;
    public bool IsPrimitiveType => Rapport.IsPrimitiveType;
    public bool IsPrimitiveTypeOrEnumOrValueType => Rapport.IsPrimitiveTypeOrEnumOrValueType;
    public bool IsValueType => Rapport.IsValueType;
    public bool IsVirtual => Rapport.IsVirtual;
    public string Name => Rapport.Name;
    public Type Type => Rapport.Type;
    public string TypeSimpleName => Rapport.TypeSimpleName;
    public ValidationAttribute[] ValidationAttributes => Rapport.ValidationAttributes;
    #endregion

    bool _NavigationDbSetLoaded;
    public DbSet? NavigationDbSet
    {
        get
        {
            if (_NavigationDbSetLoaded == false)
            {
                _NavigationDbSetLoaded = true;
                field = ParentEntity.DbSet.DbContext.DbSets
                    .FirstOrDefault(dbSet => dbSet.Type == Type);
            }
            return field;
        }
    }

    bool _ForeignKeyLoaded;
    public EntityProperty? ForeignKeyProperty
    {
        get
        {
            if (_ForeignKeyLoaded == false)
            {
                _ForeignKeyLoaded = true;
                field = ParentEntity.Properties
                    .FirstOrDefault(a => a.NavigationItemProperty == this);
            }
            return field;
        }
    }

    private string? NavigationItemPropertyName
        => IsNavigationItem
            ? ForeignKeyAttribute != null
                ? ForeignKeyAttribute.Name
                : $"{Name}Id"
            : null;
    public EntityProperty? NavigationItemProperty
    {
        get
        {
            if (field == null && IsNavigationItem)
            {
                field = ParentEntity.Properties
                    .FirstOrDefault(property => property.Name == NavigationItemPropertyName);
                if (field == null)
                    throw new Exception(
                        $"My framework is too stupid to figure out this foreign key please add a ForeignKeyAttribute " +
                        $"to property '{Name}' on entity '{ParentEntity.Name}' with the correct foreign key name.");
            }
            return field;
        }
    }

    private string? NavigationListPropertyName
        => IsNavigationList
            ? ForeignKeyAttribute != null
                ? ForeignKeyAttribute.Name
                : $"{ParentEntity.Name}Id"
            : null;
    public EntityProperty? NavigationListProperty
    {
        get
        {
            if (field == null && IsNavigationList && !NavigationDbSet!.Entity.IsHidden)
            {
                field = NavigationDbSet!.Entity.Properties
                    .FirstOrDefault(property => property.Name == NavigationListPropertyName);
                if (field == null)
                    throw new Exception(
                        $"My framework is too stupid to figure out this foreign key please add a ForeignKeyAttribute " +
                        $"to property '{Name}' on entity '{NavigationDbSet!.Entity.Name}' with the correct foreign key name.");
            }
            return field;
        }
    }

    public bool IsImmutable { get; set; }

    //public EntityProperty? StateManagedProperty
    //{
    //    get
    //    {
    //        if (ForeignKeyProperty == null) return null;
    //        if (ForeignKeyProperty.NavigationDbSet == null) return null;

    //        var item = ParentEntity.DbSet.DbContext.StateUser.Properties
    //            .FirstOrDefault(a =>
    //                a.Property.ForeignKeyProperty != null &&
    //                a.Property.ForeignKeyProperty.NavigationDbSet != null &&
    //                a.Property.ForeignKeyProperty.NavigationDbSet.Entity == ForeignKeyProperty.NavigationDbSet.Entity); // todo nakijken volgens mij kan dit gewoon Entity of nog simpeler

    //        if (item != null) return item.Property;

    //        item = ParentEntity.DbSet.DbContext.StateObjects
    //            .SelectMany(a => a.Properties)
    //            .FirstOrDefault(a =>
    //                a.Property.ForeignKeyProperty != null &&
    //                a.Property.ForeignKeyProperty.NavigationDbSet != null &&
    //                a.Property.ForeignKeyProperty.NavigationDbSet.Entity == ForeignKeyProperty.NavigationDbSet.Entity); // todo nakijken volgens mij kan dit gewoon Entity of nog simpeler

    //        return item?.Property;
    //    }
    //}

}