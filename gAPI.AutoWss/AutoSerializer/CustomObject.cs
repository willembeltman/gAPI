using Microsoft.CodeAnalysis;

namespace gAPI.AutoSerializer;

public class CustomObject
{
    public INamedTypeSymbol Type { get; set; } = default!;
    public CustomObjectMethod Writer { get; set; } = default!;
    public CustomObjectMethod Reader { get; set; } = default!;
    public CustomObjectMethod Length { get; set; } = default!;
}
