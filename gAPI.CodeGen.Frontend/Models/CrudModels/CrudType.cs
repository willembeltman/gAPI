using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.ServiceModels;
using Microsoft.CodeAnalysis;

namespace gAPI.CodeGen.Frontend.Models.CrudsModels;

/// <summary>
/// Een Crud is een verzameling van methoden die CRUD (Create, Read, Update, Delete) operaties uitvoeren op een bepaald 
/// response type. Je kunt het response type zien als een view voor verschillende methoden die op dat type werken.
/// </summary>
public class CrudType : ICrudType
{
    public CrudType(CrudContext context, Type responseType, CrudMethod[] methods)
    {
        CrudContext = context;
        ResponseType = responseType;
        Methods = methods;

        Properties = Dto?.Properties
            .Select((p, index) => new CrudProperty(this, p, index))
            .ToArray() ?? Array.Empty<CrudProperty>();

        IsStorageFileUrlProperty = Properties.Any(a => a.IsStorageFileUrlProperty);
    }

    public CrudContext CrudContext { get; }
    public Type ResponseType { get; }
    public CrudMethod[] Methods { get; }
    public bool IsStorageFileUrlProperty { get; }
    public CrudProperty[] Properties { get; }


    public string Name => Dto.Name!;
    public string Namespace => Dto.Namespace!;
    public string? FullName => Dto.FullName;

    TypeHelper? _ResponseTypeHelper { get; set; }
    public TypeHelper ResponseTypeHelper
    {
        get
        {
            _ResponseTypeHelper = _ResponseTypeHelper ?? new TypeHelper(ResponseType);
            return _ResponseTypeHelper;
        }
    }

    TypeDigger? _ResponseTypeDigger { get; set; }
    public TypeDigger ResponseTypeDigger
    {
        get
        {
            _ResponseTypeDigger = _ResponseTypeDigger ?? new TypeDigger(CrudContext.ServiceContext, ResponseType);
            return _ResponseTypeDigger;
        }
    }
    public Dto Dto => ResponseTypeDigger.Dto;
    public bool IsEntryPoint => Dto?.IsEntryPoint == true;
    public bool IsJunction => Dto?.IsJunction == true;
    public bool IsUser => Dto?.IsUser == true;
    public bool IsAuthorized => Dto?.IsAuthorized == true;
    public Type? JunctionLeftRealType { get; }
    public Type? JunctionRightRealType { get; }
    public bool IsICrudEntity => Dto.IsICrudEntity;

    CrudMethod? _ReadMethod;
    public CrudMethod? ReadMethod => _ReadMethod = _ReadMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudMethodTypeEnum.Read);

    CrudMethod? _CreateMethod;
    public CrudMethod? CreateMethod => _CreateMethod = _CreateMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudMethodTypeEnum.Create);

    CrudMethod? _UpdateMethod;
    public CrudMethod? UpdateMethod => _UpdateMethod = _UpdateMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudMethodTypeEnum.Update);

    CrudMethod? _DeleteMethod;
    public CrudMethod? DeleteMethod => _DeleteMethod = _DeleteMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudMethodTypeEnum.Delete);

    CrudMethod? _ListMethod;
    public CrudMethod? ListMethod => _ListMethod = _ListMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudMethodTypeEnum.List);

    CrudType? _JunctionLeftApi;
    public CrudType? JunctionLeftApi
    {
        get
        {
            if (_JunctionLeftApi == null && JunctionLeftRealType != null)
                _JunctionLeftApi = CrudContext.Types.FirstOrDefault(a => a.ResponseType == JunctionLeftRealType);
            return _JunctionLeftApi;
        }
    }

    CrudType? _JunctionRightApi;
    public CrudType? JunctionRightApi
    {
        get
        {
            if (_JunctionRightApi == null && JunctionRightRealType != null)
                _JunctionRightApi = CrudContext.Types.FirstOrDefault(a => a.ResponseType == JunctionRightRealType);
            return _JunctionRightApi;
        }
    }

    CrudProperty[]? _ForeignItemProperties;
    public CrudProperty[] ForeignItemProperties
        => _ForeignItemProperties = _ForeignItemProperties ?? Properties
        .Where(a => a.ReadMethod != null)
        .ToArray();

    CrudMethod[]? _ForeignListByMethods;
    public CrudMethod[] ForeignListByMethods
        => _ForeignListByMethods = _ForeignListByMethods ?? CrudContext.AllMethods
        .Where(a => a.MethodType == CrudMethodTypeEnum.ListBy && a.ForeignRealType == ResponseType)
        .ToArray();

    CrudMethod[]? _ForeignListNotByMethods;
    public CrudMethod[] ForeignListNotByMethods
        => _ForeignListNotByMethods = _ForeignListNotByMethods ?? CrudContext.AllMethods
        .Where(a => a.MethodType == CrudMethodTypeEnum.ListNotBy && a.ForeignRealType == ResponseType)
        .ToArray();

    CrudProperty? _KeyProperty;
    public CrudProperty KeyProperty
        => _KeyProperty = _KeyProperty
        ?? Properties.FirstOrDefault(a => a.IsKey)
        ?? throw new Exception($"Key is not found on type {FullName}");

    public bool IsEndPoint
        => ForeignItemProperties.Length == 0;

    ICrudProperty ICrudType.KeyProperty => KeyProperty;
    IEnumerable<ICrudProperty> ICrudType.Properties => Properties;
    IEnumerable<ICrudProperty> ICrudType.ForeignItemProperties => ForeignItemProperties;
    ICrudMethod[] ICrudType.Methods => Methods;
    ITypeDigger ICrudType.ResponseTypeDigger => ResponseTypeDigger;
    ICrudMethod? ICrudType.ReadMethod => ReadMethod;
    ICrudMethod? ICrudType.CreateMethod => CreateMethod;
    ICrudMethod? ICrudType.UpdateMethod => UpdateMethod;
    ICrudMethod? ICrudType.DeleteMethod => DeleteMethod;
    ICrudMethod? ICrudType.ListMethod => ListMethod;
    ICrudType? ICrudType.JunctionLeftApi => JunctionLeftApi;
    ICrudType? ICrudType.JunctionRightApi => JunctionRightApi;

    ITypeHelper ICrudType.ResponseType => throw new NotImplementedException();

    public string? IsPageRoute => throw new NotImplementedException();

    public string? IsPageTitle => throw new NotImplementedException();

    public string? IsPageSubmitText => throw new NotImplementedException();

    public string? IsPageResponseText => throw new NotImplementedException();

    public bool IsNotAuthorized => throw new NotImplementedException();

    public override string ToString()
    {
        return $"{ResponseType?.Name} {(IsEndPoint ? "(Endpoint)" : "") + (IsJunction ? "(Junction)" : "")}";
    }
}