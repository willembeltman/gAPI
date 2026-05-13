using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.ServiceModels;

namespace gAPI.CodeGen.Frontend.Models.CrudlsModels;


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
    public bool IsNumber => PropertyType.IsNumber;
    public bool IsDateTime => PropertyType.IsDateTime;
    public bool IsGuid => PropertyType.IsGuid;
    public bool IsCheckbox => PropertyType.IsCheckbox;
    public bool IsEnum => PropertyType.IsEnum;
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
                _ListByMethod = CrudlType.CrudlContext.AllMethods
                    .FirstOrDefault(a =>
                        a.MethodType == CrudlMethodTypeEnum.ListBy &&
                        a.ResponseRealType == CrudlType.ResponseType &&
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
    public CrudlMethod? ReadMethod
    {
        get
        {
            if (_ReadMethodLoaded == false && ListByMethod != null)
            {
                _ReadMethodLoaded = true;
                _ReadMethod = CrudlType.CrudlContext.AllMethods
                    .FirstOrDefault(a =>
                        a.MethodType == CrudlMethodTypeEnum.Read &&
                        a.ResponseRealType == ListByMethod.ForeignRealType);
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
                _ListMethod = CrudlType.CrudlContext.AllMethods
                    .FirstOrDefault(a =>
                        a.MethodType == CrudlMethodTypeEnum.List &&
                        a.ResponseRealType == ListByMethod.ForeignRealType);
            }
            return _ListMethod;
        }
    }

    public CrudlType? ForeignKeyType => ListMethod?.ResponseType;

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
                        a.DtoProperty.IsForeignNameAttribute != null &&
                        a.DtoProperty.IsForeignNameAttribute.ForeignKeyName == Name);
            }
            return _ForeignKeyNameProperty;
        }
    }

    public bool IsNullable => throw new NotImplementedException();

    ITypeHelper ICrudlProperty.PropertyType => PropertyType;
    ITypeDigger ICrudlProperty.TypeDigger => TypeDigger;
    ICrudlType? ICrudlProperty.ForeignKeyType => ForeignKeyType;
    ICrudlMethod? ICrudlProperty.ListByMethod => ListByMethod;
    ICrudlMethod? ICrudlProperty.ListMethod => ListMethod;
    ICrudlProperty? ICrudlProperty.ForeignKeyNameProperty => ForeignKeyNameProperty;

    public override string ToString()
    {
        return $"{(ReadMethod != null ? $"[READ][{ReadMethod.ResponseType!.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {DtoProperty} " +
            $"{(IsReadOnly ? "(ReadOnly)" : "") + (IsKey ? "(Key)" : "")}";
    }
}