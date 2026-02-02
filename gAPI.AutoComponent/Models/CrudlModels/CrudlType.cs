using gAPI.AutoComponent.Enums;
using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoComponent.Models.CrudlModels;

/// <summary>
/// Een Crudl is een verzameling van methoden die CRUD (Create, Read, Update, Delete) operaties uitvoeren op een bepaald 
/// response type. Je kunt het response type zien als een view voor verschillende methoden die op dat type werken.
/// </summary>
public class CrudlType : ICrudlType
{
    public CrudlType(CrudlContext context, TypeHelper responseType, CrudlMethod[] methods)
    {
        Context = context;
        ResponseType = responseType;
        Methods = methods;

        Properties = Dto?.Properties
            .Select((p, index) => new CrudlProperty(this, p, index))
            .ToArray() ?? [];
    }

    public CrudlContext Context { get; }
    public TypeHelper ResponseType { get; }
    public CrudlMethod[] Methods { get; }
    public CrudlProperty[] Properties { get; }

    public string Name => Dto?.Name ?? string.Empty;
    public string Namespace => Dto?.Namespace ?? string.Empty;
    public string FullName => Dto?.FullName ?? string.Empty;

    TypeDigger? _ResponseTypeDigger { get; set; }
    public TypeDigger ResponseTypeDigger
    {
        get
        {
            _ResponseTypeDigger ??= new TypeDigger(Context.ServiceContext, ResponseType.TypeSymbol);
            return _ResponseTypeDigger;
        }
    }
    public Dto? Dto => ResponseTypeDigger.Dto;

    public bool IsStorageFileUrlProperty => Methods.Any(a => a.InterfaceMethod.IsFileDelete || a.InterfaceMethod.IsFileUpdate);
    public bool IsEntryPoint => Dto?.IsEntryPoint == true;
    public bool IsJunction => Dto?.IsJunction == true;
    public bool IsUser => Dto?.IsUser == true;
    public bool IsAuthorized => Dto?.IsAuthorized == true;
    public bool IsICrudEntity => Dto?.IsICrudEntity == true;
    public TypeHelper? JunctionLeftRealType => Dto?.JunctionLeftRealType;
    public TypeHelper? JunctionRightRealType => Dto?.JunctionRightRealType;

    CrudlMethod? _ReadMethod;
    public CrudlMethod ReadMethod => _ReadMethod ??= Methods.FirstOrDefault(a => a.CrudlMethodType == CrudlMethodTypeEnum.Read);

    CrudlMethod? _CreateMethod;
    public CrudlMethod CreateMethod => _CreateMethod ??= Methods.FirstOrDefault(a => a.CrudlMethodType == CrudlMethodTypeEnum.Create);

    CrudlMethod? _UpdateMethod;
    public CrudlMethod UpdateMethod => _UpdateMethod ??= Methods.FirstOrDefault(a => a.CrudlMethodType == CrudlMethodTypeEnum.Update);

    CrudlMethod? _DeleteMethod;
    public CrudlMethod DeleteMethod => _DeleteMethod ??= Methods.FirstOrDefault(a => a.CrudlMethodType == CrudlMethodTypeEnum.Delete);

    CrudlMethod? _ListMethod;
    public CrudlMethod ListMethod => _ListMethod ??= Methods.FirstOrDefault(a => a.CrudlMethodType == CrudlMethodTypeEnum.List);


    CrudlType? _JunctionLeftApi;
    public CrudlType? JunctionLeftApi
    {
        get
        {
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            if (_JunctionLeftApi == null && JunctionLeftRealType != null)
                _JunctionLeftApi = Context.Crudls.FirstOrDefault(a => a.ResponseType == JunctionLeftRealType);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8604 // Possible null reference argument.
            return _JunctionLeftApi;
        }
    }

    CrudlType? _JunctionRightApi;
    public CrudlType? JunctionRightApi
    {
        get
        {
#pragma warning disable CS8604 // Possible null reference argument.
            if (_JunctionRightApi == null && JunctionRightRealType != null!)
                _JunctionRightApi = Context.Crudls.FirstOrDefault(a => a.ResponseType == JunctionRightRealType);
#pragma warning restore CS8604 // Possible null reference argument.
            return _JunctionRightApi;
        }
    }

    CrudlProperty[]? _ForeignItemProperties;
    public CrudlProperty[] ForeignItemProperties
        => _ForeignItemProperties ??= Properties
        .Where(a => a.ForeignKey_ReadMethod != null)
        .ToArray();

    CrudlMethod[]? _ForeignListMethods;
    public CrudlMethod[] ForeignListMethods
#pragma warning disable CS8604 // Possible null reference argument.
        => _ForeignListMethods ??= Context.AllCrudlMethods
        .Where(a => a.CrudlMethodType == CrudlMethodTypeEnum.ListBy && a.ForeignRealType == ResponseType)
        .ToArray();
#pragma warning restore CS8604 // Possible null reference argument.

    CrudlProperty? _KeyProperty;
    public CrudlProperty KeyProperty
        => _KeyProperty ??= Properties.FirstOrDefault(a => a.IsKey);

    public bool IsEndPoint
        => ForeignItemProperties.Length == 0;

    ICrudlProperty ICrudlType.KeyProperty => KeyProperty;
    IEnumerable<ICrudlProperty> ICrudlType.Properties => Properties;
    IEnumerable<ICrudlProperty> ICrudlType.ForeignItemProperties => ForeignItemProperties;

    ICrudlMethod[] ICrudlType.Methods => Methods;
    ITypeHelper ICrudlType.ResponseTypeHelper => ResponseType;
    ITypeDigger ICrudlType.ResponseTypeDigger => ResponseTypeDigger;
    ICrudlMethod? ICrudlType.ReadMethod => ReadMethod;
    ICrudlMethod? ICrudlType.CreateMethod => CreateMethod;
    ICrudlMethod? ICrudlType.UpdateMethod => UpdateMethod;
    ICrudlMethod? ICrudlType.DeleteMethod => DeleteMethod;
    ICrudlMethod? ICrudlType.ListMethod => ListMethod;
    ICrudlType? ICrudlType.JunctionLeftApi => JunctionLeftApi;
    ICrudlType? ICrudlType.JunctionRightApi => JunctionRightApi;

    public override string ToString()
    {
        return $"{ResponseType?.Name} {(IsEndPoint ? "(Endpoint)" : "") + (IsJunction ? "(Junction)" : "")}";
    }
}