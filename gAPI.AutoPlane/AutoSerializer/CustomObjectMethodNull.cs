using Microsoft.CodeAnalysis;

namespace gAPI.AutoSerializer;

public class CustomObjectMethodNull
{
    public INamedTypeSymbol StaticClass { get; set; } = default!;
    public IMethodSymbol Method { get; set; } = default!;
    public INamedTypeSymbol? Type { get; set; } = default!;
}
