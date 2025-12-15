using gAPI.AutoComponents.Interfaces;
using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.ServiceModels;
using Microsoft.CodeAnalysis;

namespace gAPI.CodeGen.Frontend.Models.CrudlsModels
{
    /// <summary>
    /// Een Crudl is een verzameling van methoden die CRUD (Create, Read, Update, Delete) operaties uitvoeren op een bepaald 
    /// response type. Je kunt het response type zien als een view voor verschillende methoden die op dat type werken.
    /// </summary>
    public class CrudlType : ICrudlType
    {
        public CrudlType(CrudlContext context, Type responseType, CrudlMethod[] methods)
        {
            CrudlContext = context;
            ResponseType = responseType;
            Methods = methods;

            Properties = Dto?.Properties
                .Select((p, index) => new CrudlProperty(this, p, index))
                .ToArray() ?? Array.Empty<CrudlProperty>();

            IsStorageFile = Properties.Any(a => a.IsStorageFile);
        }

        public CrudlContext CrudlContext { get; }
        public Type ResponseType { get; }
        public CrudlMethod[] Methods { get; }
        public bool IsStorageFile { get; }
        public CrudlProperty[] Properties { get; }


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
                _ResponseTypeDigger = _ResponseTypeDigger ?? new TypeDigger(CrudlContext.ServiceContext, ResponseType);
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

        CrudlMethod? _ReadMethod;
        public CrudlMethod? ReadMethod => _ReadMethod = _ReadMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudlMethodTypeEnum.Read);

        CrudlMethod? _CreateMethod;
        public CrudlMethod? CreateMethod => _CreateMethod = _CreateMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudlMethodTypeEnum.Create);

        CrudlMethod? _UpdateMethod;
        public CrudlMethod? UpdateMethod => _UpdateMethod = _UpdateMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudlMethodTypeEnum.Update);

        CrudlMethod? _DeleteMethod;
        public CrudlMethod? DeleteMethod => _DeleteMethod = _DeleteMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudlMethodTypeEnum.Delete);

        CrudlMethod? _ListMethod;
        public CrudlMethod? ListMethod => _ListMethod = _ListMethod ?? Methods.FirstOrDefault(a => a.MethodType == CrudlMethodTypeEnum.List);

        CrudlType? _JunctionLeftApi;
        public CrudlType? JunctionLeftApi
        {
            get
            {
                if (_JunctionLeftApi == null && JunctionLeftRealType != null)
                    _JunctionLeftApi = CrudlContext.Types.FirstOrDefault(a => a.ResponseType == JunctionLeftRealType);
                return _JunctionLeftApi;
            }
        }

        CrudlType? _JunctionRightApi;
        public CrudlType? JunctionRightApi
        {
            get
            {
                if (_JunctionRightApi == null && JunctionRightRealType != null)
                    _JunctionRightApi = CrudlContext.Types.FirstOrDefault(a => a.ResponseType == JunctionRightRealType);
                return _JunctionRightApi;
            }
        }

        CrudlProperty[]? _ForeignItemProperties;
        public CrudlProperty[] ForeignItemProperties
            => _ForeignItemProperties = _ForeignItemProperties ?? Properties
            .Where(a => a.ReadMethod != null)
            .ToArray();

        CrudlMethod[]? _ForeignListByMethods;
        public CrudlMethod[] ForeignListByMethods
            => _ForeignListByMethods = _ForeignListByMethods ?? CrudlContext.AllMethods
            .Where(a => a.MethodType == CrudlMethodTypeEnum.ListBy && a.ForeignRealType == ResponseType)
            .ToArray();

        CrudlMethod[]? _ForeignListNotByMethods;
        public CrudlMethod[] ForeignListNotByMethods
            => _ForeignListNotByMethods = _ForeignListNotByMethods ?? CrudlContext.AllMethods
            .Where(a => a.MethodType == CrudlMethodTypeEnum.ListNotBy && a.ForeignRealType == ResponseType)
            .ToArray();

        CrudlProperty? _KeyProperty;
        public CrudlProperty KeyProperty
            => _KeyProperty = _KeyProperty
            ?? Properties.FirstOrDefault(a => a.IsKey)
            ?? throw new Exception($"Key is not found on type {FullName}");

        public bool IsEndPoint
            => ForeignItemProperties.Length == 0;

        ICrudlProperty ICrudlType.KeyProperty => KeyProperty;
        IEnumerable<ICrudlProperty> ICrudlType.Properties => Properties;
        IEnumerable<ICrudlProperty> ICrudlType.ForeignItemProperties => ForeignItemProperties;
        ICrudlMethod[] ICrudlType.Methods => Methods;
        ITypeHelper ICrudlType.ResponseTypeHelper => ResponseTypeHelper;
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
}