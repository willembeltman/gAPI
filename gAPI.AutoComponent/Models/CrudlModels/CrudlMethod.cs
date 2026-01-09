using gAPI.AutoComponent.Enums;
using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using System.Linq;

namespace gAPI.AutoComponent.Models.CrudlModels;

public class CrudlMethod : ICrudlMethod
{
    public CrudlMethod(
        CrudlContext serviceContext,
        Interface @interface,
        InterfaceMethod interfaceMethod,
        CrudlMethodTypeEnum crudlMethodType,
        TypeHelper responeType)
    {
        Context = serviceContext;
        Interface = @interface;
        InterfaceMethod = interfaceMethod;
        CrudlMethodType = crudlMethodType;
        Type = responeType;
    }

    public CrudlContext Context { get; }
    public Interface Interface { get; }
    public InterfaceMethod InterfaceMethod { get; }
    public CrudlMethodTypeEnum CrudlMethodType { get; }
    public TypeHelper Type { get; } // Het response type wordt uiteindelijk de Crudl zelf.

    public string Name => InterfaceMethod.Name;
    public InterfaceMethodArgument[] Arguments => InterfaceMethod.Arguments;
    public Client Client => Interface.Client;
    public bool HasIFormFile => Arguments.Any(a => a.IsIFormFile);
    public bool IsAuthorized => InterfaceMethod.IsAuthorized;
    public bool IsNullable => InterfaceMethod.IsNullable;
    public string? IsPageRoute => InterfaceMethod.IsPageRoute;
    public string? ForeignKeyName => InterfaceMethod.IsListByName;
    public TypeHelper? ForeignRealType => InterfaceMethod.IsListByForeignType;


    CrudlType? _ResponseType;
    public CrudlType ResponseType
        => _ResponseType = _ResponseType ?? Context.Crudls
            .FirstOrDefault(a => a.ResponseType == Type);

    CrudlType? _ForeignType;
    public CrudlType ForeignType
        => _ForeignType = _ForeignType ?? Context.Crudls
            .FirstOrDefault(a => a.ResponseType == ForeignRealType!);

    TypeDigger? _ResponseTypeDigger;
    public TypeDigger ResponseTypeDigger
    {
        get
        {
            _ResponseTypeDigger = _ResponseTypeDigger ?? new TypeDigger(Context.ServiceContext, Type.TypeSymbol, IsNullable);
            return _ResponseTypeDigger;
        }
    }

    ISharedReference ICrudlMethod.Interface => Interface;
    ISharedReference ICrudlMethod.Client => Client;

    public override string ToString()
    {
        return $"[{CrudlMethodType.ToString().ToUpper()}]" +
            $"{(ForeignType != null ? $"[{ForeignType.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {Type?.Name} {Name}";
    }
}