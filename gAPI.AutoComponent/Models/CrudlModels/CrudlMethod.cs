using gAPI.AutoComponent.Enums;
using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using System.Linq;

namespace gAPI.AutoComponent.Models.CrudlModels;

public class CrudlMethod(
    CrudlContext serviceContext,
    Interface @interface,
    InterfaceMethod interfaceMethod,
    CrudlMethodTypeEnum crudlMethodType,
    TypeHelper type)
    : ICrudlMethod
{
    public CrudlContext Context { get; } = serviceContext;
    public Interface Interface { get; } = @interface;
    public InterfaceMethod InterfaceMethod { get; } = interfaceMethod;
    public CrudlMethodTypeEnum CrudlMethodType { get; } = crudlMethodType;
    public TypeHelper Type { get; } = type;

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

    TypeDigger? TypeDiggerInner { get; set; }
    public TypeDigger TypeDigger
        => TypeDiggerInner ??= new TypeDigger(Context.ServiceContext, Type.TypeSymbol, IsNullable);

    CrudlType? _ForeignType;
    public CrudlType ForeignType
        => _ForeignType ??= Context.Crudls
            .FirstOrDefault(a => a.ResponseType.FullName == IsListByForeignType?.FullName);

    ISharedReference ICrudlMethod.Interface => Interface;
    ITypeHelper ICrudlMethod.Type => Type;
    ICrudlMethodArgument[] ICrudlMethod.Arguments => Arguments;
    ITypeDigger ICrudlMethod.TypeDigger => TypeDigger;

    public override string ToString()
    {
        return $"[{CrudlMethodType.ToString().ToUpper()}]" +
            $"{(ForeignType != null ? $"[{ForeignType.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {CrudlType?.Name} {Name}";
    }
}