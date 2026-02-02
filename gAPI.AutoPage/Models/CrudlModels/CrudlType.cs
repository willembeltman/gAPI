using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models.ServiceModels;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.Models.CrudlModels;

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

        foreach (var method in methods)
        {
            method.CrudlType = this;
        }


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

    TypeDigger? ResponseTypeDiggerInner { get; set; }
    public TypeDigger ResponseTypeDigger
    {
        get
        {
            ResponseTypeDiggerInner ??= new TypeDigger(Context.ServiceContext, ResponseType.TypeSymbol);
            return ResponseTypeDiggerInner;
        }
    }
    public Dto? Dto => ResponseTypeDigger.Dto;

    public bool IsStorageFileUrlProperty => Methods.Any(a => a.InterfaceMethod.IsFileDelete || a.InterfaceMethod.IsFileUpdate);
    public bool IsEntryPoint => Dto?.IsEntryPoint == true;
    public bool IsJunction => Dto?.IsJunction == true;
    public bool IsUser => Dto?.IsUser == true;
    public bool IsAuthorized => Dto?.IsAuthorized == true;
    public bool IsICrudEntity => Dto?.IsICrudEntity == true;
    public string? IsPageRoute => Methods.FirstOrDefault(a => a.IsPageRoute != null)?.IsPageRoute;
    public string? IsPageTitle => Methods.FirstOrDefault(a => a.IsPageTitle != null)?.IsPageTitle;
    public string? IsPageSubmitText => Methods.FirstOrDefault(a => a.IsPageSubmitText != null)?.IsPageSubmitText;
    public string? IsPageResponseText => Methods.FirstOrDefault(a => a.IsPageResponseText != null)?.IsPageResponseText;

    public bool IsNotAuthorized => throw new System.NotImplementedException();
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
            if (_JunctionLeftApi == null && JunctionLeftRealType != null)
                _JunctionLeftApi = Context.Crudls.FirstOrDefault(a => a.ResponseType.FullName == JunctionLeftRealType.FullName);
            return _JunctionLeftApi;
        }
    }

    CrudlType? _JunctionRightApi;
    public CrudlType? JunctionRightApi
    {
        get
        {
            if (_JunctionRightApi == null && JunctionRightRealType != null!)
                _JunctionRightApi = Context.Crudls.FirstOrDefault(a => a.ResponseType.FullName == JunctionRightRealType.FullName);
            return _JunctionRightApi;
        }
    }

    CrudlProperty[]? _ForeignItemProperties;
    public CrudlProperty[] ForeignItemProperties
        => _ForeignItemProperties ??= [.. Properties.Where(a => a.ForeignKey_ReadMethod != null)];

    CrudlMethod[]? _ForeignListMethods;
    public CrudlMethod[] ForeignListMethods
        => _ForeignListMethods ??= [.. Context.AllCrudlMethods.Where(a => a.CrudlMethodType == CrudlMethodTypeEnum.ListBy && a.IsListByForeignType?.FullName == ResponseType.FullName)];

    CrudlProperty? _KeyProperty;
    public CrudlProperty KeyProperty
        => _KeyProperty ??= Properties.FirstOrDefault(a => a.IsKey);

    public bool IsEndPoint
        => ForeignItemProperties.Length == 0;

    ICrudlProperty ICrudlType.KeyProperty => KeyProperty;
    IEnumerable<ICrudlProperty> ICrudlType.Properties => Properties;
    IEnumerable<ICrudlProperty> ICrudlType.ForeignItemProperties => ForeignItemProperties;

    ICrudlMethod[] ICrudlType.Methods => Methods;
    ITypeHelper ICrudlType.ResponseType => ResponseType;
    ITypeDigger ICrudlType.ResponseTypeDigger => ResponseTypeDigger;
    ICrudlMethod? ICrudlType.ReadMethod => ReadMethod;
    ICrudlMethod? ICrudlType.CreateMethod => CreateMethod;
    ICrudlMethod? ICrudlType.UpdateMethod => UpdateMethod;
    ICrudlMethod? ICrudlType.DeleteMethod => DeleteMethod;
    ICrudlMethod? ICrudlType.ListMethod => ListMethod;
    ICrudlType? ICrudlType.JunctionLeftApi => JunctionLeftApi;
    ICrudlType? ICrudlType.JunctionRightApi => JunctionRightApi;

    IDto? ICrudlType.Dto => Dto;

    public override string ToString()
    {
        return $"{ResponseType?.Name} {(IsEndPoint ? "(Endpoint)" : "") + (IsJunction ? "(Junction)" : "")}";
    }
}