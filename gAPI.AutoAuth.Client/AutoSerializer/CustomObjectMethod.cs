using Microsoft.CodeAnalysis;

namespace gAPI.AutoSerializer;

public class CustomObjectMethod
{
    public CustomObjectMethod(CustomObjectMethodNull a)
    {
        StaticClass = a.StaticClass;
        Method = a.Method;
        Type = a.Type!;
    }

    public INamedTypeSymbol StaticClass { get; set; } = default!;
    public IMethodSymbol Method { get; set; } = default!;
    public INamedTypeSymbol Type { get; set; } = default!;
}
