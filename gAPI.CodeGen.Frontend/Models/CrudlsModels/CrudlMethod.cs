using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.ServiceModels;

namespace gAPI.CodeGen.Frontend.Models.CrudlsModels;

public class CrudlMethod : ICrudlMethod
{
    public CrudlMethod(
        CrudlContext serviceContext,
        Interface @interface,
        InterfaceMethod interfaceMethod,
        CrudlMethodTypeEnum crudlMethodType,
        Type responeType)
    {
        Context = serviceContext;
        Interface = @interface;
        InterfaceMethod = interfaceMethod;
        MethodType = crudlMethodType;
        ResponseRealType = responeType;
    }




    ICrudlMethodArgument[] ICrudlMethod.Arguments => Arguments;

    public AutoComponent.Enums.CrudlMethodTypeEnum CrudlMethodType => throw new NotImplementedException();

    public bool IsNotAuthorized => throw new NotImplementedException();

    public string? IsPageTitle => throw new NotImplementedException();

    public string? IsPageSubmitText => throw new NotImplementedException();

    public string? IsPageResponseText => throw new NotImplementedException();

    public string? IsComponentTitle => throw new NotImplementedException();

    public string? IsComponentSubmitText => throw new NotImplementedException();

    public string? IsComponentResponseText => throw new NotImplementedException();






    public CrudlContext Context { get; }
    public Interface Interface { get; }
    public InterfaceMethod InterfaceMethod { get; }
    public CrudlMethodTypeEnum MethodType { get; }
    public Type ResponseRealType { get; } // Het response type wordt uiteindelijk de Crudl zelf.

    public string Name => InterfaceMethod.Name;
    public InterfaceMethodArgument[] Arguments => InterfaceMethod.Arguments;
    //public Client Client => Interface.Client!;
    public bool HasIFormFile => Arguments.Any(a => a.IsIFormFile);
    public bool IsAuthorized => InterfaceMethod.IsAuthorized;
    public bool IsNullable => InterfaceMethod.IsNullable;
    public string? IsPageRoute => InterfaceMethod.IsPageRoute;
    public string? ForeignKeyName => InterfaceMethod.IsListByName;
    public Type? ForeignRealType => InterfaceMethod.IsListByForeignType;


    CrudlType? _ResponseType;
    public CrudlType? ResponseType
        => _ResponseType = _ResponseType ?? Context.Types
            .FirstOrDefault(a => a.ResponseType == ResponseRealType);

    CrudlType? _ForeignType;
    public CrudlType? ForeignType
        => _ForeignType = _ForeignType ?? Context.Types
            .FirstOrDefault(a => a.ResponseType == ForeignRealType);

    TypeDigger? TypeDiggerInternal { get; set; }
    public TypeDigger TypeDigger
    {
        get
        {
            TypeDiggerInternal = TypeDiggerInternal ?? new TypeDigger(Context.ServiceContext, ResponseRealType, IsNullable);
            return TypeDiggerInternal;
        }
    }

    //ISharedReference ICrudlMethod.Client => Client;
    ISharedReference ICrudlMethod.Interface => Interface;

    public ITypeHelper Type => throw new NotImplementedException();

    ITypeDigger ICrudlMethod.TypeDigger => TypeDigger;

    public override string ToString()
    {
        return $"[{MethodType.ToString().ToUpper()}]" +
            $"{(ForeignType != null ? $"[{ForeignType.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {ResponseRealType?.Name} {Name}";
    }
}