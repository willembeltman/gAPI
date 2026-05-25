using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models.ServiceModels;
using System.Linq;

namespace gAPI.AutoPage.Models.CrudModels;


public class CrudProperty : ICrudProperty
{
    public CrudProperty(CrudType parentType, TypeHelperProperty dtoProperty)
    {
        CrudType = parentType;
        DtoProperty = dtoProperty;
    }

    public CrudType CrudType { get; }
    public TypeHelperProperty DtoProperty { get; }
    public string Name => DtoProperty.Name;
    public bool IsReadOnly => DtoProperty.IsReadOnly;
    public bool IsForeignName => DtoProperty.IsForeignName;
    public bool IsStateManaged => DtoProperty.IsStateManaged;
    public bool IsImmutable => DtoProperty.IsImmutable;
    public bool IsStorageFileUrlProperty => DtoProperty.IsStorageFileUrlProperty;
    public bool IsKey => DtoProperty.IsKey;
    public bool IsName => DtoProperty.IsName;
    public ITypeHelper PropertyType => DtoProperty.Type;
    public ITypeDigger TypeDigger => DtoProperty.TypeDigger;
    public bool IsNumber => TypeDigger.Type.IsNumber;
    public bool IsDateTime => TypeDigger.Type.IsDateTime;
    public bool IsCheckbox => TypeDigger.Type.IsCheckbox;
    public bool IsEnum => TypeDigger.Type.IsEnum;
    public string TypeSimpleName => PropertyType.Name;

    bool _ListByMethodLoaded;
    CrudMethod? _ListByMethod;
    /// <summary>
    /// Zoekt de <c>ListBy</c>-methode op in de service van het huidige dto-type, op basis van een foreign key.
    /// Bijvoorbeeld: <c>User.CurrentCompanyId</c> verwijst naar <c>UserService.LoadByCurrentCompanyId</c>.
    /// Deze methode wordt gebruikt om te bepalen naar welk foreign type de foreign key verwijst.
    /// </summary>
    public CrudMethod? ListByMethod
    {
        get
        {
            if (_ListByMethodLoaded == false)
            {
                _ListByMethodLoaded = true;
                _ListByMethod = CrudType.Methods
                    .FirstOrDefault(a =>
                        a.CrudMethodType == CrudMethodTypeEnum.ListBy &&
                        a.ForeignKeyName == DtoProperty.Name);
            }
            return _ListByMethod;
        }
    }

    bool _ReadMethodLoaded;
    CrudMethod? _ReadMethod;
    /// <summary>
    /// Zoekt de <c>Read</c>-methode op in de service van het foreign dto-type.
    /// Deze methode kan worden gebruikt om een individueel object op te halen op basis van de foreign key.
    /// Bijvoorbeeld: <c>User.CurrentCompanyId</c> verwijst naar <c>CompanyService.Read</c>.
    public CrudMethod? ForeignKey_ReadMethod
    {
        get
        {
            if (_ReadMethodLoaded == false && ListByMethod != null)
            {
                _ReadMethodLoaded = true;
                _ReadMethod = CrudType.Context.AllCrudMethods
                    .FirstOrDefault(a =>
                        a.CrudMethodType == CrudMethodTypeEnum.Read &&
                        a.CrudType!.ResponseType.FullName == ListByMethod.IsListByForeignType!.FullName);
            }
            return _ReadMethod;
        }
    }

    bool _ListMethodLoaded;
    CrudMethod? _ListMethod;
    /// <summary>
    /// Zoekt de <c>List</c>-methode op in de service van het foreign type.
    /// Deze methode kan worden gebruikt om alle objecten van het foreign type op te halen.
    /// Bijvoorbeeld: <c>User.CurrentCompanyId</c> verwijst naar <c>CompanyService.List</c>.
    /// </summary>
    public CrudMethod? ListMethod
    {
        get
        {
            if (_ListMethodLoaded == false && ListByMethod != null)
            {
                _ListMethodLoaded = true;
                _ListMethod = CrudType.Context.AllCrudMethods
                    .FirstOrDefault(a =>
                        a.CrudMethodType == CrudMethodTypeEnum.List &&
                        a.CrudType!.ResponseType.FullName == ListByMethod.IsListByForeignType!.FullName);
            }
            return _ListMethod;
        }
    }

    public CrudType? ForeignKeyType => ListMethod?.CrudType;

    private bool _ForeignKeyNamePropertyLoaded { get; set; }
    private CrudProperty? _ForeignKeyNameProperty { get; set; }
    public CrudProperty? ForeignKeyNameProperty
    {
        get
        {
            if (_ForeignKeyNamePropertyLoaded == false)
            {
                _ForeignKeyNameProperty = CrudType.Properties
                    .FirstOrDefault(a =>
                        a.DtoProperty.IsForeignName &&
                        a.DtoProperty.IsForeignNameString == Name);
            }
            return _ForeignKeyNameProperty;
        }
    }

    public bool IsNullable => PropertyType.IsNullable;

    ICrudMethod? ICrudProperty.ListByMethod => ListByMethod;
    ICrudType? ICrudProperty.ForeignKeyType => ForeignKeyType;
    ICrudMethod? ICrudProperty.ListMethod => ListMethod;
    ICrudProperty? ICrudProperty.ForeignKeyNameProperty => ForeignKeyNameProperty;
    ITypeHelper ICrudProperty.PropertyType => PropertyType;
    ITypeDigger ICrudProperty.TypeDigger => TypeDigger;


    public override string ToString()
    {
        return $"{(ForeignKey_ReadMethod != null ? $"[READ][{ForeignKey_ReadMethod.CrudType!.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {DtoProperty} " +
            $"{(IsReadOnly ? "(ReadOnly)" : "") + (IsKey ? "(Key)" : "")}";
    }
}