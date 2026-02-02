using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models.ServiceModels;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoPage.Models.CrudlModels;


public class CrudlProperty : ICrudlProperty
{
    public CrudlProperty(CrudlType parentType, DtoProperty dtoProperty, int index)
    {
        CrudlType = parentType;
        DtoProperty = dtoProperty;
        Index = index;
    }

    public CrudlType CrudlType { get; }
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
    public bool IsNumber => TypeDigger.Type.IsNumber;
    public bool IsDateTime => TypeDigger.Type.IsDateTime;
    public bool IsCheckbox => TypeDigger.Type.IsCheckbox;
    public bool IsEnum => TypeDigger.Type.IsEnum;
    public string TypeSimpleName => PropertyType.Name;

    bool _ListByMethodLoaded;
    CrudlMethod? _ListByMethod;
    /// <summary>
    /// Zoekt de <c>ListBy</c>-methode op in de service van het huidige dto-type, op basis van een foreign key.
    /// Bijvoorbeeld: <c>User.CurrentCompanyId</c> verwijst naar <c>UserService.LoadByCurrentCompanyId</c>.
    /// Deze methode wordt gebruikt om te bepalen naar welk foreign type de foreign key verwijst.
    /// </summary>
    public CrudlMethod? ListByMethod
    {
        get
        {
            if (_ListByMethodLoaded == false)
            {
                _ListByMethodLoaded = true;
                _ListByMethod = CrudlType.Methods
                    .FirstOrDefault(a =>
                        a.CrudlMethodType == CrudlMethodTypeEnum.ListBy &&
                        a.ForeignKeyName == DtoProperty.Name);
            }
            return _ListByMethod;
        }
    }

    bool _ReadMethodLoaded;
    CrudlMethod? _ReadMethod;
    /// <summary>
    /// Zoekt de <c>Read</c>-methode op in de service van het foreign dto-type.
    /// Deze methode kan worden gebruikt om een individueel object op te halen op basis van de foreign key.
    /// Bijvoorbeeld: <c>User.CurrentCompanyId</c> verwijst naar <c>CompanyService.Read</c>.
    public CrudlMethod? ForeignKey_ReadMethod
    {
        get
        {
            if (_ReadMethodLoaded == false && ListByMethod != null)
            {
                _ReadMethodLoaded = true;
                _ReadMethod = CrudlType.Context.AllCrudlMethods
                    .FirstOrDefault(a =>
                        a.CrudlMethodType == CrudlMethodTypeEnum.Read &&
                        a.CrudlType.ResponseType.FullName == ListByMethod.IsListByForeignType!.FullName);
            }
            return _ReadMethod;
        }
    }

    bool _ListMethodLoaded;
    CrudlMethod? _ListMethod;
    /// <summary>
    /// Zoekt de <c>List</c>-methode op in de service van het foreign type.
    /// Deze methode kan worden gebruikt om alle objecten van het foreign type op te halen.
    /// Bijvoorbeeld: <c>User.CurrentCompanyId</c> verwijst naar <c>CompanyService.List</c>.
    /// </summary>
    public CrudlMethod? ListMethod
    {
        get
        {
            if (_ListMethodLoaded == false && ListByMethod != null)
            {
                _ListMethodLoaded = true;
                _ListMethod = CrudlType.Context.AllCrudlMethods
                    .FirstOrDefault(a =>
                        a.CrudlMethodType == CrudlMethodTypeEnum.List &&
                        a.CrudlType.ResponseType.FullName == ListByMethod.IsListByForeignType!.FullName);
            }
            return _ListMethod;
        }
    }

    public CrudlType? ForeignKeyType => ListMethod?.CrudlType;

    private bool _ForeignKeyNamePropertyLoaded { get; set; }
    private CrudlProperty? _ForeignKeyNameProperty { get; set; }
    public CrudlProperty? ForeignKeyNameProperty
    {
        get
        {
            if (_ForeignKeyNamePropertyLoaded == false)
            {
                _ForeignKeyNameProperty = CrudlType.Properties
                    .FirstOrDefault(a =>
                        a.DtoProperty.IsForeignName &&
                        a.DtoProperty.IsForeignNameString == Name);
            }
            return _ForeignKeyNameProperty;
        }
    }

    public bool IsNullable => PropertyType.IsNullable;

    ICrudlMethod? ICrudlProperty.ListByMethod => ListByMethod;
    ICrudlType? ICrudlProperty.ForeignKeyType => ForeignKeyType;
    ICrudlMethod? ICrudlProperty.ListMethod => ListMethod;
    ICrudlProperty? ICrudlProperty.ForeignKeyNameProperty => ForeignKeyNameProperty;
    ITypeHelper ICrudlProperty.PropertyType => PropertyType;
    ITypeDigger ICrudlProperty.TypeDigger => TypeDigger;


    public override string ToString()
    {
        return $"{(ForeignKey_ReadMethod != null ? $"[READ][{ForeignKey_ReadMethod.CrudlType.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {DtoProperty} " +
            $"{(IsReadOnly ? "(ReadOnly)" : "") + (IsKey ? "(Key)" : "")}";
    }
}