using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models.ServiceModels;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoPage.Models.CrudlModels;

public class CrudlMethod(
    CrudlContext serviceContext,
    Interface @interface,
    InterfaceMethod interfaceMethod,
    CrudlMethodTypeEnum crudlMethodType,
    TypeHelper responeType) : ICrudlMethod
{
    public CrudlContext Context { get; } = serviceContext;
    public Interface Interface { get; } = @interface;
    public InterfaceMethod InterfaceMethod { get; } = interfaceMethod;
    public CrudlMethodTypeEnum CrudlMethodType { get; } = crudlMethodType;
    public TypeHelper ResponseType { get; } = responeType;

    public CrudlType? CrudlType { get; set; }

    public string Name => InterfaceMethod.Name;
    public InterfaceMethodArgument[] Arguments => InterfaceMethod.Arguments;
    public Client Client => Interface.Client;
    public bool HasIFormFile => Arguments.Any(a => a.IsIFormFile);
    public bool IsAuthorized => InterfaceMethod.IsAuthorized;
    public bool IsNullable => InterfaceMethod.IsNullable;
    public string? ForeignKeyName => InterfaceMethod.IsListByName;
    public TypeHelper? IsListByForeignType => InterfaceMethod.IsListByForeignType;
    public bool IsNotAuthorized => InterfaceMethod.IsNotAuthorized;

    public string? IsPageRoute => InterfaceMethod.IsPageRoute;
    public string? IsPageTitle => InterfaceMethod.IsPageTitle;
    public string? IsPageSubmitText => InterfaceMethod.IsPageSubmitText;
    public string? IsPageResponseText => InterfaceMethod.IsPageResponseText;
    public string? IsComponentTitle => InterfaceMethod.IsComponentTitle;
    public string? IsComponentSubmitText => InterfaceMethod.IsComponentSubmitText;
    public string? IsComponentResponseText => InterfaceMethod.IsComponentResponseText;


    CrudlType? _ForeignType;
    public CrudlType ForeignType
        => _ForeignType ??= Context.Crudls
            .FirstOrDefault(a => a.ResponseType.FullName == IsListByForeignType?.FullName);

    TypeDigger? _ResponseTypeDigger;
    public TypeDigger ResponseTypeDigger
    {
        get
        {
            _ResponseTypeDigger ??= new TypeDigger(Context.ServiceContext, ResponseType.TypeSymbol, IsNullable);
            return _ResponseTypeDigger;
        }
    }

    ISharedReference ICrudlMethod.Interface => Interface;
    ISharedReference ICrudlMethod.Client => Client;
    ISharedReference ICrudlMethod.ResponseTypeDigger => ResponseTypeDigger;
    ITypeHelper ICrudlMethod.ResponseType => ResponseType;
    ICrudlMethodArgument[] ICrudlMethod.Arguments => Arguments;


    public override string ToString()
    {
        return $"[{CrudlMethodType.ToString().ToUpper()}]" +
            $"{(ForeignType != null ? $"[{ForeignType.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {CrudlType?.Name} {Name}";
    }
}