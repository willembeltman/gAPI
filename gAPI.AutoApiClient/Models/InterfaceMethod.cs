using gAPI.AutoApiClient.Contexts;
using gAPI.AutoApiClient.Enums;
using gAPI.AutoApiClient.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoApiClient.Models;

internal class InterfaceMethod
{
    public InterfaceMethod(ServiceContext dataModel, Interface @interface, IMethodSymbol methodSymbol)
    {
        DataModel = dataModel;
        Interface = @interface;
        MethodSymbol = methodSymbol;

        Name = methodSymbol.Name;

        IsNullable =
            methodSymbol.ReturnNullableAnnotation == NullableAnnotation.Annotated;

        ResponseTypeSymbol = methodSymbol.ReturnType;
        ResponseType = new TypeHelper(dataModel, ResponseTypeSymbol, IsNullable);

        ApiName = Name;
        var apiNameAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ApiNameAttribute");
        if (apiNameAttr != null)
        {
            ApiName = apiNameAttr.ConstructorArguments[0].Value?.ToString() ?? ApiName;
        }


        Arguments = methodSymbol.Parameters
            .Select(parameterSymbol => new InterfaceMethodArgument(dataModel, this, parameterSymbol))
            .ToArray();

        IsCreate = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsCreateAttribute");
        if (IsCreate && Arguments.Length != 1)
            throw new Exception("Kan niet create method hebben met anders dan 1 parameter");

        IsRead = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsReadAttribute");
        if (IsRead && Arguments.Length != 1)
            throw new Exception("Kan niet read method hebben met anders dan 1 parameter");

        IsUpdate = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsUpdateAttribute");
        if (IsUpdate && Arguments.Length != 1)
            throw new Exception("Kan niet update method hebben met anders dan 1 parameter");

        var isDeleteAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsFileDeleteAttribute");

        if (isDeleteAttr != null)
        {
            IsFileDelete = true;

            var arg = isDeleteAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol targetTypeSymbol)
            {
                IsFileDeleteType = new TypeHelper(dataModel, targetTypeSymbol);
            }

            if (methodSymbol.Parameters.Length != 1)
                throw new Exception("Kan niet file delete method hebben met anders dan 1 parameter");
        }

        IsList = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsListAttribute");
        if (IsList && Arguments.Length != 3)
            throw new Exception("Kan niet list method hebben met anders dan 3 parameters");

        var isListByAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsListByAttribute");
        if (isListByAttr != null)
        {
            IsListBy = true;
            IsListByName = isListByAttr.ConstructorArguments[0].Value?.ToString();
            if (isListByAttr.ConstructorArguments[1].Kind == TypedConstantKind.Type &&
                isListByAttr.ConstructorArguments[1].Value is ITypeSymbol targetTypeSymbol)
            {
                IsListByForeignType = new TypeHelper(dataModel, targetTypeSymbol);
            }

            if (Arguments.Length != 4)
                throw new Exception("Kan niet delete method hebben met anders dan 1 parameter");
        }

        IsFileUpdate = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsFileUpdateAttribute");
        if (IsFileUpdate && Arguments.Length != 2)
            throw new Exception("Kan niet file update method hebben met anders dan 2 parameter");

        var isFileDeleteAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsFileDeleteAttribute");

        if (isFileDeleteAttr != null)
        {
            IsFileDelete = true;

            var arg = isFileDeleteAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol targetTypeSymbol)
            {
                IsFileDeleteType = new TypeHelper(dataModel, targetTypeSymbol);
            }

            if (methodSymbol.Parameters.Length != 1)
                throw new Exception("Kan niet file delete method hebben met anders dan 1 parameter");
        }

        var isPageAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsPageAttribute");
        if (isPageAttr != null)
        {
            IsPage = true;
            IsPageRoute = isPageAttr.ConstructorArguments[0].Value?.ToString();
        }

        IsAuthorized =
            @interface.IsAuthorized ||
            methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsAuthorizedAttribute");

        IsHidden =
            @interface.IsHidden ||
            methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsHiddenAttribute");

        IsAsync = ResponseType._Name == "Task";
    }

    public ServiceContext DataModel { get; }
    public Interface Interface { get; }
    public IMethodSymbol MethodSymbol { get; }
    public ITypeSymbol ResponseTypeSymbol { get; }
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
    public TypeHelper IsDeleteType { get; }
    public bool IsList { get; }
    public bool IsListBy { get; }
    public string IsListByName { get; }
    public TypeHelper IsListByForeignType { get; }
    public bool IsFileUpdate { get; }
    public bool IsFileDelete { get; }
    public TypeHelper IsFileDeleteType { get; }
    public bool IsPage { get; }
    public string IsPageRoute { get; }

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
            else if (IsPage)
                return CrudlMethodTypeEnum.Page;
            else
                return CrudlMethodTypeEnum.NotSet;
        }
    }


    TypeDigger _ResponseTypeDigger { get; set; }
    public TypeDigger ResponseTypeDigger
    {
        get
        {
            _ResponseTypeDigger = _ResponseTypeDigger ?? new TypeDigger(DataModel, ResponseTypeSymbol, IsNullable);
            return _ResponseTypeDigger;
        }
    }
}