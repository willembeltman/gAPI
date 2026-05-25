using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.ServiceModels;

namespace gAPI.CodeGen.Frontend.Models.CrudsModels;


public class CrudProperty : ICrudProperty
{
    public CrudProperty(CrudType parentType, DtoProperty dtoProperty, int index)
    {
        CrudType = parentType;
        DtoProperty = dtoProperty;
        Index = index;
    }

    public CrudType CrudType { get; }
    public DtoProperty DtoProperty { get; }
    public string Name => DtoProperty.Name;
    public int Index { get; }
    public bool IsReadOnly => DtoProperty.IsReadOnly;
    public bool IsForeignName => DtoProperty.IsForeignName;
    public bool IsStateManaged => DtoProperty.IsStateManaged;
    public bool IsImmutable => DtoProperty.IsImmutable;
    public bool IsStorageFileUrlProperty => DtoProperty.IsStorageFileUrlProperty;
    public bool IsKey => DtoProperty.IsKey;
    public bool IsName => DtoProperty.IsName;

    public TypeHelper PropertyType => DtoProperty.PropertyType;
    public TypeDigger TypeDigger => DtoProperty.TypeDigger;
    public bool IsNumber => PropertyType.IsNumber;
    public bool IsDateTime => PropertyType.IsDateTime;
    public bool IsGuid => PropertyType.IsGuid;
    public bool IsCheckbox => PropertyType.IsCheckbox;
    public bool IsEnum => PropertyType.IsEnum;
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
                _ListByMethod = CrudType.CrudContext.AllCrudMethods
                    .FirstOrDefault(a =>
                        a.MethodType == CrudMethodTypeEnum.ListBy &&
                        a.ResponseRealType == CrudType.ResponseType &&
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
    public CrudMethod? ReadMethod
    {
        get
        {
            if (_ReadMethodLoaded == false && ListByMethod != null)
            {
                _ReadMethodLoaded = true;
                _ReadMethod = CrudType.CrudContext.AllCrudMethods
                    .FirstOrDefault(a =>
                        a.MethodType == CrudMethodTypeEnum.Read &&
                        a.ResponseRealType == ListByMethod.ForeignRealType);
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
                _ListMethod = CrudType.CrudContext.AllCrudMethods
                    .FirstOrDefault(a =>
                        a.MethodType == CrudMethodTypeEnum.List &&
                        a.ResponseRealType == ListByMethod.ForeignRealType);
            }
            return _ListMethod;
        }
    }

    public CrudType? ForeignKeyType => ListMethod?.ResponseType;

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
                        a.DtoProperty.IsForeignNameAttribute != null &&
                        a.DtoProperty.IsForeignNameAttribute.ForeignKeyName == Name);
            }
            return _ForeignKeyNameProperty;
        }
    }

    public bool IsNullable => throw new NotImplementedException();

    ITypeHelper ICrudProperty.PropertyType => PropertyType;
    ITypeDigger ICrudProperty.TypeDigger => TypeDigger;
    ICrudType? ICrudProperty.ForeignKeyType => ForeignKeyType;
    ICrudMethod? ICrudProperty.ListByMethod => ListByMethod;
    ICrudMethod? ICrudProperty.ListMethod => ListMethod;
    ICrudProperty? ICrudProperty.ForeignKeyNameProperty => ForeignKeyNameProperty;

    public override string ToString()
    {
        return $"{(ReadMethod != null ? $"[READ][{ReadMethod.ResponseType!.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {DtoProperty} " +
            $"{(IsReadOnly ? "(ReadOnly)" : "") + (IsKey ? "(Key)" : "")}";
    }
}