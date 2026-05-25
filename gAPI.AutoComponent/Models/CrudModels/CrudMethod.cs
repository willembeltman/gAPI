using gAPI.AutoComponent.Enums;
using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using System.Linq;

namespace gAPI.AutoComponent.Models.CrudModels;

public class CrudMethod(
    CrudContext serviceContext,
    Interface @interface,
    InterfaceMethod interfaceMethod,
    CrudMethodTypeEnum crudMethodType,
    TypeHelper type)
    : ICrudMethod
{
    public CrudContext Context { get; } = serviceContext;
    public Interface Interface { get; } = @interface;
    public InterfaceMethod InterfaceMethod { get; } = interfaceMethod;
    public CrudMethodTypeEnum CrudMethodType { get; } = crudMethodType;
    public TypeHelper Type { get; } = type;

    public CrudType? CrudType { get; set; }

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

    TypeDigger? TypeDiggerInner { get; set; }
    public TypeDigger TypeDigger
        => TypeDiggerInner ??= new TypeDigger(Context.ServiceContext, Type.TypeSymbol, IsNullable);

    CrudType? _ForeignType;
    public CrudType ForeignType
        => _ForeignType ??= Context.Cruds
            .FirstOrDefault(a => a.ResponseType.FullName == IsListByForeignType?.FullName);

    ISharedReference ICrudMethod.Interface => Interface;
    ITypeHelper ICrudMethod.Type => Type;
    ICrudMethodArgument[] ICrudMethod.Arguments => Arguments;
    ITypeDigger ICrudMethod.TypeDigger => TypeDigger;

    public override string ToString()
    {
        return $"[{CrudMethodType.ToString().ToUpper()}]" +
            $"{(ForeignType != null ? $"[{ForeignType.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {CrudType?.Name} {Name}";
    }
}