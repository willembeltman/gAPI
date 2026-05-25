using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.ServiceModels;

namespace gAPI.CodeGen.Frontend.Models.CrudsModels;

public class CrudMethod : ICrudMethod
{
    public CrudMethod(
        CrudContext serviceContext,
        Interface @interface,
        InterfaceMethod interfaceMethod,
        CrudMethodTypeEnum crudMethodType,
        Type responeType)
    {
        Context = serviceContext;
        Interface = @interface;
        InterfaceMethod = interfaceMethod;
        MethodType = crudMethodType;
        ResponseRealType = responeType;
    }




    ICrudMethodArgument[] ICrudMethod.Arguments => Arguments;

    public AutoComponent.Enums.CrudMethodTypeEnum CrudMethodType => throw new NotImplementedException();

    public bool IsNotAuthorized => throw new NotImplementedException();

    public string? IsPageTitle => throw new NotImplementedException();

    public string? IsPageSubmitText => throw new NotImplementedException();

    public string? IsPageResponseText => throw new NotImplementedException();

    public string? IsComponentTitle => throw new NotImplementedException();

    public string? IsComponentSubmitText => throw new NotImplementedException();

    public string? IsComponentResponseText => throw new NotImplementedException();






    public CrudContext Context { get; }
    public Interface Interface { get; }
    public InterfaceMethod InterfaceMethod { get; }
    public CrudMethodTypeEnum MethodType { get; }
    public Type ResponseRealType { get; } // Het response type wordt uiteindelijk de Crud zelf.

    public string Name => InterfaceMethod.Name;
    public InterfaceMethodArgument[] Arguments => InterfaceMethod.Arguments;
    //public Client Client => Interface.Client!;
    public bool HasIFormFile => Arguments.Any(a => a.IsIFormFile);
    public bool IsAuthorized => InterfaceMethod.IsAuthorized;
    public bool IsNullable => InterfaceMethod.IsNullable;
    public string? IsPageRoute => InterfaceMethod.IsPageRoute;
    public string? ForeignKeyName => InterfaceMethod.IsListByName;
    public Type? ForeignRealType => InterfaceMethod.IsListByForeignType;


    CrudType? _ResponseType;
    public CrudType? ResponseType
        => _ResponseType = _ResponseType ?? Context.AllCrudTypes
            .FirstOrDefault(a => a.ResponseType == ResponseRealType);

    CrudType? _ForeignType;
    public CrudType? ForeignType
        => _ForeignType = _ForeignType ?? Context.AllCrudTypes
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

    //ISharedReference ICrudMethod.Client => Client;
    ISharedReference ICrudMethod.Interface => Interface;

    public ITypeHelper Type => throw new NotImplementedException();

    ITypeDigger ICrudMethod.TypeDigger => TypeDigger;

    public override string ToString()
    {
        return $"[{MethodType.ToString().ToUpper()}]" +
            $"{(ForeignType != null ? $"[{ForeignType.ResponseType.Name.ToUpper()}]" : "")}" +
            $" {ResponseRealType?.Name} {Name}";
    }
}