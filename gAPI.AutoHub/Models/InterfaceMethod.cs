using gAPI.AutoHub.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoHub.Models;

internal class InterfaceMethod
{
    public InterfaceMethod(ServiceContext dataModel, Interface @interface, IMethodSymbol methodSymbol)
    {
        Interface = @interface;
        MethodSymbol = methodSymbol;

        Name = methodSymbol.Name;

        IsNullable =
            methodSymbol.ReturnNullableAnnotation == NullableAnnotation.Annotated;

        ResponseType = new TypeHelper(dataModel, methodSymbol.ReturnType, IsNullable);

        Title = Name;
        var TitleAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TitleAttribute");
        if (TitleAttribute != null)
        {
            Title = TitleAttribute.ConstructorArguments[0].Value?.ToString() ?? Title;
        }

        Arguments = methodSymbol.Parameters
            .Select(parameterSymbol => new InterfaceMethodArgument(dataModel, this, parameterSymbol))
            .ToArray();

        IsAuthorized =
            @interface.IsAuthorized ||
            methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsAuthorizedAttribute");

        IsHidden =
            @interface.IsHidden ||
            methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsHiddenAttribute");

        IsAsync = ResponseType.NameInner == "Task";
    }

    public Interface Interface { get; }
    public IMethodSymbol MethodSymbol { get; }
    public string Name { get; }
    public bool IsNullable { get; }
    public TypeHelper ResponseType { get; }
    public string Title { get; }
    public InterfaceMethodArgument[] Arguments { get; }
    public bool IsAsync { get; }
    public bool IsAuthorized { get; }
    public bool IsHidden { get; }
}