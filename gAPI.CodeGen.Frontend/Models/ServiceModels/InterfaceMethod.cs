using gAPI.Attributes;
using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Enums;
using gAPI.CodeGen.Frontend.Helpers;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels;

public class InterfaceMethod
{
    public InterfaceMethod(ServiceContext dataModel, Interface @interface, MethodInfo method)
    {
        DataModel = dataModel;
        Interface = @interface;
        MethodInfo = method;

        Name = method.Name;

        var context = new NullabilityInfoContext();
        var nullabilityInfo = context.Create(method.ReturnParameter);
        IsNullable = nullabilityInfo.ReadState == NullabilityState.Nullable;

        ResponseRealType = method.ReturnType;
        ResponseType = new TypeHelper(ResponseRealType, IsNullable);

        ApiName = Name;
        var apiNameAttr = method.GetCustomAttribute<ApiNameAttribute>();
        if (apiNameAttr != null)
        {
            ApiName = apiNameAttr.Name ?? ApiName;
        }

        Arguments = method.GetParameters()
            .Select(parameterInfo => new InterfaceMethodArgument(dataModel, this, parameterInfo))
            .ToArray();

        IsCreate = method.GetCustomAttribute<IsCreateAttribute>() != null;
        if (IsCreate && Arguments.Length != 1)
            throw new Exception("Kan niet create method hebben met anders dan 1 parameter");

        IsRead = method.GetCustomAttribute<IsReadAttribute>() != null;
        if (IsRead && Arguments.Length != 1)
            throw new Exception("Kan niet read method hebben met anders dan 1 parameter");

        IsUpdate = method.GetCustomAttribute<IsUpdateAttribute>() != null;
        if (IsUpdate && Arguments.Length != 1)
            throw new Exception("Kan niet update method hebben met anders dan 1 parameter");

        var isDeleteAttr = method.CustomAttributes
            .FirstOrDefault(b => b.AttributeType == typeof(IsDeleteAttribute));
        if (isDeleteAttr != null)
        {
            IsDeleteType = (Type)isDeleteAttr.ConstructorArguments[0].Value!;
            IsDelete = true;
            if (Arguments.Length != 1)
                throw new Exception("Kan niet delete method hebben met anders dan 1 parameter");
        }

        IsList = method.GetCustomAttribute<IsListAttribute>() != null;
        if (IsList && Arguments.Length != 3)
            throw new Exception("Kan niet list method hebben met anders dan 3 parameters");

        var isListByAttr = method.CustomAttributes
            .FirstOrDefault(b => b.AttributeType == typeof(IsListByAttribute));
        if (isListByAttr != null)
        {
            IsListBy = true;
            IsListByName = isListByAttr.ConstructorArguments[0].Value?.ToString();
            IsListByForeignType = (Type)isListByAttr.ConstructorArguments[1].Value!;
            if (Arguments.Length != 4)
                throw new Exception("Kan niet delete method hebben met anders dan 1 parameter");
        }

        var isListNotByAttr = method.CustomAttributes
            .FirstOrDefault(b => b.AttributeType == typeof(IsListNotByAttribute));
        if (isListNotByAttr != null)
        {
            IsListNotBy = true;
            IsListNotByName = isListNotByAttr.ConstructorArguments[0].Value?.ToString();
            IsListNotByForeignType = (Type)isListNotByAttr.ConstructorArguments[1].Value!;
            if (Arguments.Length != 4)
                throw new Exception("Kan niet delete method hebben met anders dan 1 parameter");
        }

        var isPageAttr = method.CustomAttributes
            .FirstOrDefault(b => b.AttributeType == typeof(IsPageAttribute));
        if (isPageAttr != null)
        {
            IsPage = true;
            IsPageRoute = isPageAttr.ConstructorArguments[0].Value?.ToString();
        }

        IsFileUpdate = method.GetCustomAttribute<IsFileUpdateAttribute>() != null;
        if (IsFileUpdate && Arguments.Length != 2)
            throw new Exception("Kan niet file update method hebben met anders dan 2 parameter");

        var isFileDeleteAttr = method.CustomAttributes
            .FirstOrDefault(b => b.AttributeType == typeof(IsFileDeleteAttribute));
        if (isFileDeleteAttr != null)
        {
            IsFileDeleteType = (Type)isFileDeleteAttr.ConstructorArguments[0].Value!;
            IsFileDelete = true;
            if (Arguments.Length != 1)
                throw new Exception("Kan niet delete method hebben met anders dan 1 parameter");
        }

        IsAuthorized =
            Interface.IsAuthorized ||
            method.GetCustomAttribute<IsAuthorizedAttribute>() != null;

        IsHidden =
            @interface.IsHidden ||
            method.GetCustomAttribute<IsHiddenAttribute>() != null;

        IsAsync = ResponseType._Name == "Task";
    }

    public ServiceContext DataModel { get; }
    public Interface Interface { get; }
    public MethodInfo MethodInfo { get; }
    public Type ResponseRealType { get; }
    public TypeHelper ResponseType { get; }
    public string Name { get; }
    public string ApiName { get; }
    public InterfaceMethodArgument[] Arguments { get; }

    public bool IsNullable { get; }
    public bool IsAuthorized { get; }
    public bool IsHidden { get; }
    public bool IsAsync { get; }

    public bool IsCreate { get; }
    public bool IsRead { get; }
    public bool IsUpdate { get; }
    public bool IsDelete { get; }
    public Type? IsDeleteType { get; }
    public bool IsList { get; }
    public bool IsListBy { get; }
    public string? IsListByName { get; }
    public Type? IsListByForeignType { get; }
    public bool IsListNotBy { get; }
    public string? IsListNotByName { get; }
    public Type? IsListNotByForeignType { get; }
    public bool IsFileUpdate { get; }
    public bool IsFileDelete { get; }
    public Type? IsFileDeleteType { get; }
    public bool IsPage { get; }
    public string? IsPageRoute { get; }

    public CrudlMethodTypeEnum CrudlMethodType
    {
        get
        {
            if (IsCreate)
                return CrudlMethodTypeEnum.Create;
            else if (IsRead)
                return CrudlMethodTypeEnum.Read;
            else if (IsUpdate)
                return CrudlMethodTypeEnum.Update;
            else if (IsDelete)
                return CrudlMethodTypeEnum.Delete;
            else if (IsList)
                return CrudlMethodTypeEnum.List;
            else if (IsListBy)
                return CrudlMethodTypeEnum.ListBy;
            else if (IsListNotBy)
                return CrudlMethodTypeEnum.ListNotBy;
            else if (IsPage)
                return CrudlMethodTypeEnum.Page;
            else
                return CrudlMethodTypeEnum.NotSet;
        }
    }


    TypeDigger? _ResponseTypeDigger { get; set; }
    public TypeDigger ResponseTypeDigger
    {
        get
        {
            _ResponseTypeDigger = _ResponseTypeDigger ?? new TypeDigger(DataModel, ResponseRealType, IsNullable);
            return _ResponseTypeDigger;
        }
    }
}