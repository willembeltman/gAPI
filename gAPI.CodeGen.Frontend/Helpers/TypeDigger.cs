using gAPI.AutoComponent.Interfaces;
using gAPI.CodeGen.Frontend.Models;
using gAPI.CodeGen.Frontend.Models.ServiceModels;
using Microsoft.CodeAnalysis;

namespace gAPI.CodeGen.Frontend.Helpers;

public class TypeDigger : ITypeDigger
{
    public TypeDigger(ServiceContext context, Type type, bool isNullable = false)
    {
        StartType = type ?? throw new ArgumentNullException(nameof(type));
        Context = context ?? throw new ArgumentNullException(nameof(context));

        Type = DigTillBase(type);

        Name = Type.Name;
        FullName = Type.FullName ?? Type.Name;
        Namespace = Type.Namespace ?? "";
        IsValueType = Type.IsValueType;
        IsNullable = isNullable;

        (Dto, Enum) = FindDtoOrEnum(context, FullName);

    }

    private (Dto Dto, EnumDto Enum) FindDtoOrEnum(ServiceContext context, string fullName)
    {
        var foundDto = context.Dtos.FirstOrDefault(dto => dto.FullName == fullName);
        var foundEnum = foundDto == null
            ? context.Enums.FirstOrDefault(e => e.FullName == fullName)
            : null;

        return (foundDto, foundEnum)!;
    }

    private Type DigTillBase(Type type)
    {
        var resolved = type;

        while (true)
        {
            if (IsArrayType(resolved, out var elementType))
                resolved = elementType;

            if (!IsGenericType(resolved!))
                break;

            var next = GetUnderlyingTypeGeneric(resolved!);

            if (AreTypesEqual(next, resolved!))
                break;

            resolved = next;
        }

        return resolved!;
    }

    private bool IsArrayType(Type type, out Type? elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }
        elementType = null;
        return false;
    }

    private bool IsGenericType(Type type)
    {
        return type.IsGenericType;
    }

    private Type GetUnderlyingTypeGeneric(Type type)
    {
        // 1. Nullable<T> → T
        if (Nullable.GetUnderlyingType(type) is Type nullableInner)
            return nullableInner;

        // 2. IEnumerable<T>, List<T>, Task<T>, ActionResult<T>, etc.
        var args = type.GetGenericArguments();
        if (args.Length == 1)
            return args[0];

        // 3. Arrays → element type (veiligheidshalve)
        if (type.IsArray)
            return type.GetElementType()!;

        // 4. Complexere generics → pak laatste argument
        if (args.Length > 1)
            return args.Last();

        // Geen onderliggend type gevonden
        return type;
    }

    private bool AreTypesEqual(Type next, Type resolved)
    {
        return next == resolved;
    }

    public Dto Dto { get; }
    public EnumDto Enum { get; }
    public Type StartType { get; }
    public ServiceContext Context { get; }
    public Type Type { get; }
    public string Name { get; }
    public string FullName { get; }
    public string Namespace { get; }
    public bool IsValueType { get; }
    public bool IsNullable { get; }

    ITypeHelper ITypeDigger.Type => throw new NotImplementedException();

    ITypeHelper ITypeDigger.StartType => throw new NotImplementedException();
}