using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoApi.Client.Models;

public class InterfaceMethodArgument
{
    public InterfaceMethodArgument(ServiceContext dataModel, InterfaceMethod serviceMethod, IParameterSymbol parameterSymbol)
    {
        DataModel = dataModel;
        ServiceMethod = serviceMethod;
        ParameterSymbol = parameterSymbol;

        Name = parameterSymbol.Name;

        IsPassword = parameterSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsPasswordAttribute");
        IsNullable =
            parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        IsIFormFile = ParameterSymbol.Type.Name == "IFormFile";
        IsValueType = parameterSymbol.Type.IsValueType;
    }

    public ServiceContext DataModel { get; }
    public InterfaceMethod ServiceMethod { get; }
    public IParameterSymbol ParameterSymbol { get; }
    public string Name { get; }
    public bool IsPassword { get; }
    public bool IsNullable { get; }
    public bool IsIFormFile { get; }
    public bool IsValueType { get; }

    TypeHelper? ParameterTypeInner { get; set; }
    public TypeHelper ParameterType => ParameterTypeInner ??= new TypeHelper(DataModel, ParameterSymbol.Type, IsNullable);

    public static bool IsEnumerableOrAsyncEnumerable(IParameterSymbol parameterSymbol)
    {
        if (parameterSymbol?.Type is not INamedTypeSymbol typeSymbol)
        {
            // Controleer of het een Array is (Arrays implementeren ook IEnumerable)
            return parameterSymbol?.Type is IArrayTypeSymbol;
        }

        // 1. Controleer of het type zelf de interface is
        if (typeSymbol.SpecialType == SpecialType.System_Collections_IEnumerable ||
            typeSymbol.MetadataName == "IAsyncEnumerable`1")
        {
            return true;
        }

        // 2. Controleer alle geïmplementeerde interfaces (voor List, Queue, HashSet, etc.)
        return typeSymbol.AllInterfaces.Any(i =>
            i.SpecialType == SpecialType.System_Collections_IEnumerable ||
            i.MetadataName == "IAsyncEnumerable`1");
    }

    public static bool IsArrayOrList(IParameterSymbol parameterSymbol)
    {
        var type = parameterSymbol?.Type;
        if (type == null) return false;

        // 1. Check of het een Array is
        if (type is IArrayTypeSymbol) return true;

        // 2. Check of het een List<T> of List is (via MetadataName)
        return type.MetadataName == "List`1" && type.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic";
    }


    public override string ToString()
    {
        return Name;
    }
}