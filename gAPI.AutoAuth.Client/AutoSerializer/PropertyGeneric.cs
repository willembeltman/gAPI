using Microsoft.CodeAnalysis;

#nullable enable

namespace gAPI.AutoSerializer;

public class PropertyGeneric
{
    public PropertyGeneric(IPropertySymbol property, bool isFromGenericParent)
    {
        Property = property;
        IsFromGenericParent = isFromGenericParent;
    }
    public IPropertySymbol Property { get; }
    public bool IsFromGenericParent { get; }
    public bool IsBool => Property.Type.SpecialType == SpecialType.System_Boolean;
}
