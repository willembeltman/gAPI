using gAPI.AutoPage.Enums;
using gAPI.AutoPage.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoPage.Models.ServiceModels;

public class InterfaceMethod
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

        Title = Name;
        var TitleAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TitleAttribute");
        if (TitleAttribute != null)
        {
            Title = TitleAttribute.ConstructorArguments[0].Value?.ToString() ?? Title;
        }


        Arguments = [.. methodSymbol.Parameters.Select(parameterSymbol => new InterfaceMethodArgument(dataModel, this, parameterSymbol))];
        IsCreate = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsCreateAttribute");
        if (IsCreate && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 1)
            throw new Exception("Kan niet create method hebben met anders dan 1 parameter");

        IsRead = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsReadAttribute");
        if (IsRead && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 1)
            throw new Exception("Kan niet read method hebben met anders dan 1 parameter");

        IsUpdate = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsUpdateAttribute");
        if (IsUpdate && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 1)
            throw new Exception("Kan niet update method hebben met anders dan 1 parameter");

        var isDeleteAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsDeleteAttribute");
        if (isDeleteAttr != null)
        {
            IsDelete = true;

            var arg = isDeleteAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol targetTypeSymbol)
            {
                IsDeleteType = new TypeHelper(dataModel, targetTypeSymbol);
            }
        }
        if (IsDelete && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 1)
            throw new Exception("Kan niet file delete method hebben met anders dan 1 parameter");

        IsList = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsListAttribute");
        if (IsList && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 3)
            throw new Exception("Kan niet List method hebben met anders dan 3 parameters");

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
        }
        if (IsListBy && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 4)
            throw new Exception("Kan niet ListBy method hebben met anders dan 4 parameter");

        var isListNotByAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsListNotByAttribute");
        if (isListNotByAttr != null)
        {
            IsListNotBy = true;
            IsListNotByName = isListNotByAttr.ConstructorArguments[0].Value?.ToString();
            if (isListNotByAttr.ConstructorArguments[1].Kind == TypedConstantKind.Type &&
                isListNotByAttr.ConstructorArguments[1].Value is ITypeSymbol targetTypeSymbol)
            {
                IsListNotByForeignType = new TypeHelper(dataModel, targetTypeSymbol);
            }
        }
        if (IsListNotBy && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 4)
            throw new Exception("Kan niet delete method hebben met anders dan 4 parameter");

        IsFileUpdate = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsFileUpdateAttribute");
        if (IsFileUpdate && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 2)
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
        }
        if (IsFileDelete && Arguments.Count(a => a.ParameterType.Name != "CancellationToken") != 1)
            throw new Exception("Kan niet file delete method hebben met anders dan 1 parameter");

        var isPageAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsPageAttribute");
        if (isPageAttr != null)
        {
            IsPage = true;
            IsPageRoute = isPageAttr.ConstructorArguments[0].Value?.ToString();
            IsPageTitle = isPageAttr.ConstructorArguments[1].Value?.ToString();
            IsPageSubmitText = isPageAttr.ConstructorArguments.Length > 2 ? isPageAttr.ConstructorArguments[2].Value?.ToString() : null;
            IsPageResponseText = isPageAttr.ConstructorArguments.Length > 3 ? isPageAttr.ConstructorArguments[3].Value?.ToString() : null;
        }

        var isComponentAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "IsComponentAttribute");
        if (isComponentAttr != null)
        {
            IsComponent = true;
            IsComponentTitle = isComponentAttr.ConstructorArguments[0].Value?.ToString();
            IsComponentSubmitText = isComponentAttr.ConstructorArguments.Length > 1 ? isComponentAttr.ConstructorArguments[1].Value?.ToString() : null;
            IsComponentResponseText = isComponentAttr.ConstructorArguments.Length > 2 ? isComponentAttr.ConstructorArguments[2].Value?.ToString() : null;
        }

        IsAuthorized =
            @interface.IsAuthorized ||
            methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsAuthorizedAttribute");

        IsNotAuthorized =
            @interface.IsNotAuthorized ||
            methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsNotAuthorizedAttribute");

        IsHidden =
            @interface.IsHidden ||
            methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsHiddenAttribute");

        IsAsync = ResponseType.NameInner == "Task";
    }

    public ServiceContext DataModel { get; }
    public Interface Interface { get; }
    public IMethodSymbol MethodSymbol { get; }
    public ITypeSymbol ResponseTypeSymbol { get; }
    public TypeHelper ResponseType { get; }
    public string Name { get; }
    public string Title { get; }
    public InterfaceMethodArgument[] Arguments { get; }

    public bool IsNullable { get; }
    public bool IsAuthorized { get; }
    public bool IsNotAuthorized { get; }
    public bool IsHidden { get; }
    public bool IsAsync { get; }

    public bool IsCreate { get; }
    public bool IsRead { get; }
    public bool IsUpdate { get; }
    public bool IsDelete { get; }
    public TypeHelper? IsDeleteType { get; }
    public bool IsList { get; }
    public bool IsListBy { get; }
    public string? IsListByName { get; }
    public TypeHelper? IsListByForeignType { get; }
    public bool IsListNotBy { get; }
    public string? IsListNotByName { get; }
    public TypeHelper? IsListNotByForeignType { get; }
    public bool IsFileUpdate { get; }
    public bool IsFileDelete { get; }
    public TypeHelper? IsFileDeleteType { get; }
    public bool IsPage { get; }
    public string? IsPageRoute { get; }
    public string? IsPageTitle { get; }
    public string? IsPageSubmitText { get; }
    public string? IsPageResponseText { get; }
    public bool IsComponent { get; }
    public string? IsComponentTitle { get; }
    public string? IsComponentSubmitText { get; }
    public string? IsComponentResponseText { get; }

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
                return CrudlMethodTypeEnum.IsPage;
            else if (IsComponent)
                return CrudlMethodTypeEnum.IsComponent;
            else if (IsFileUpdate)
                return CrudlMethodTypeEnum.FileUpdate;
            else if (IsFileDelete)
                return CrudlMethodTypeEnum.FileDelete;
            else
                return CrudlMethodTypeEnum.NotSet;
        }
    }


    TypeDigger? ResponseTypeDiggerInner { get; set; }
    public TypeDigger ResponseTypeDigger => ResponseTypeDiggerInner ??= new TypeDigger(DataModel, ResponseTypeSymbol, IsNullable);

}