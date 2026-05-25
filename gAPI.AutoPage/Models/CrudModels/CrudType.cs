using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models.ServiceModels;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.Models.CrudModels;

/// <summary>
/// Een Crud is een verzameling van methoden die CRUD (Create, Read, Update, Delete) operaties uitvoeren op een bepaald 
/// response type. Je kunt het response type zien als een view voor verschillende methoden die op dat type werken.
/// </summary>
public class CrudType : ICrudType
{
    public CrudType(CrudContext context, TypeHelper responseType, CrudMethod[] methods)
    {
        Context = context;
        ResponseType = responseType;
        Methods = methods;

        foreach (var method in methods)
        {
            method.CrudType = this;
        }

        Properties = ResponseTypeBase?.GetProperties()
            .Select(p => new CrudProperty(this, p))
            .ToArray() ?? [];
    }

    public CrudContext Context { get; }
    public TypeHelper ResponseType { get; }
    public CrudMethod[] Methods { get; }
    public CrudProperty[] Properties { get; }

    public string Name => ResponseTypeBase?.Name ?? string.Empty;
    public string Namespace => ResponseTypeBase?.Namespace ?? string.Empty;
    public string FullName => ResponseTypeBase?.FullName ?? string.Empty;

    TypeDigger? ResponseTypeDiggerInner { get; set; }
    public TypeDigger ResponseTypeDigger => ResponseTypeDiggerInner ??= new TypeDigger(Context.ServiceContext, ResponseType.TypeSymbol);
    public TypeHelper? ResponseTypeBase => ResponseTypeDigger.Type;

    public bool IsStorageFileUrlProperty => Methods.Any(a => a.InterfaceMethod.IsFileDelete || a.InterfaceMethod.IsFileUpdate);
    public bool IsEntryPoint => ResponseTypeBase?.IsEntryPoint == true;
    public bool IsJunction => ResponseTypeBase?.IsJunction == true;
    public bool IsUser => ResponseTypeBase?.IsUser == true;
    public bool IsAuthorized => ResponseTypeBase?.IsAuthorized == true;
    public bool IsICrudEntity => ResponseTypeBase?.IsICrudEntity == true;
    public string? IsPageRoute => Methods.FirstOrDefault(a => a.IsPageRoute != null)?.IsPageRoute;
    public string? IsPageTitle => Methods.FirstOrDefault(a => a.IsPageTitle != null)?.IsPageTitle;
    public string? IsPageSubmitText => Methods.FirstOrDefault(a => a.IsPageSubmitText != null)?.IsPageSubmitText;
    public string? IsPageResponseText => Methods.FirstOrDefault(a => a.IsPageResponseText != null)?.IsPageResponseText;

    public bool IsNotAuthorized => throw new System.NotImplementedException();
    public ITypeHelper? JunctionLeftRealType => ResponseTypeBase?.JunctionLeftRealType;
    public ITypeHelper? JunctionRightRealType => ResponseTypeBase?.JunctionRightRealType;

    CrudMethod? _ReadMethod;
    public CrudMethod ReadMethod => _ReadMethod ??= Methods.FirstOrDefault(a => a.CrudMethodType == CrudMethodTypeEnum.Read);

    CrudMethod? _CreateMethod;
    public CrudMethod CreateMethod => _CreateMethod ??= Methods.FirstOrDefault(a => a.CrudMethodType == CrudMethodTypeEnum.Create);

    CrudMethod? _UpdateMethod;
    public CrudMethod UpdateMethod => _UpdateMethod ??= Methods.FirstOrDefault(a => a.CrudMethodType == CrudMethodTypeEnum.Update);

    CrudMethod? _DeleteMethod;
    public CrudMethod DeleteMethod => _DeleteMethod ??= Methods.FirstOrDefault(a => a.CrudMethodType == CrudMethodTypeEnum.Delete);

    CrudMethod? _ListMethod;
    public CrudMethod ListMethod => _ListMethod ??= Methods.FirstOrDefault(a => a.CrudMethodType == CrudMethodTypeEnum.List);


    CrudType? _JunctionLeftApi;
    public CrudType? JunctionLeftApi
    {
        get
        {
            if (_JunctionLeftApi == null && JunctionLeftRealType != null)
                _JunctionLeftApi = Context.Cruds.FirstOrDefault(a => a.ResponseType.FullName == JunctionLeftRealType.FullName);
            return _JunctionLeftApi;
        }
    }

    CrudType? _JunctionRightApi;
    public CrudType? JunctionRightApi
    {
        get
        {
            if (_JunctionRightApi == null && JunctionRightRealType != null!)
                _JunctionRightApi = Context.Cruds.FirstOrDefault(a => a.ResponseType.FullName == JunctionRightRealType.FullName);
            return _JunctionRightApi;
        }
    }

    CrudProperty[]? _ForeignItemProperties;
    public CrudProperty[] ForeignItemProperties
        => _ForeignItemProperties ??= [.. Properties.Where(a => a.ForeignKey_ReadMethod != null)];

    CrudMethod[]? _ForeignListMethods;
    public CrudMethod[] ForeignListMethods
        => _ForeignListMethods ??= [.. Context.AllCrudMethods.Where(a => a.CrudMethodType == CrudMethodTypeEnum.ListBy && a.IsListByForeignType?.FullName == ResponseType.FullName)];

    CrudProperty? _KeyProperty;
    public CrudProperty KeyProperty
        => _KeyProperty ??= Properties.FirstOrDefault(a => a.IsKey);

    public bool IsEndPoint
        => ForeignItemProperties.Length == 0;

    ICrudProperty ICrudType.KeyProperty => KeyProperty;
    IEnumerable<ICrudProperty> ICrudType.Properties => Properties;
    IEnumerable<ICrudProperty> ICrudType.ForeignItemProperties => ForeignItemProperties;

    ICrudMethod[] ICrudType.Methods => Methods;
    ITypeHelper ICrudType.ResponseType => ResponseType;
    ITypeDigger ICrudType.ResponseTypeDigger => ResponseTypeDigger;
    ICrudMethod? ICrudType.ReadMethod => ReadMethod;
    ICrudMethod? ICrudType.CreateMethod => CreateMethod;
    ICrudMethod? ICrudType.UpdateMethod => UpdateMethod;
    ICrudMethod? ICrudType.DeleteMethod => DeleteMethod;
    ICrudMethod? ICrudType.ListMethod => ListMethod;
    ICrudType? ICrudType.JunctionLeftApi => JunctionLeftApi;
    ICrudType? ICrudType.JunctionRightApi => JunctionRightApi;

    public override string ToString()
    {
        return $"{ResponseType?.Name} {(IsEndPoint ? "(Endpoint)" : "") + (IsJunction ? "(Junction)" : "")}";
    }
}