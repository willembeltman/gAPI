using System.Collections.Generic;

namespace gAPI.AutoComponent.Interfaces;

public interface ICrudlType : ISharedReference
{
    bool IsUser { get; }
    bool IsAuthorized { get; }
    bool IsStorageFileUrlProperty { get; }
    bool IsICrudEntity { get; }
    bool IsEntryPoint { get; }
    bool IsJunction { get; }
    ICrudlProperty KeyProperty { get; }
    IEnumerable<ICrudlProperty> Properties { get; }
    IEnumerable<ICrudlProperty> ForeignItemProperties { get; }
    ICrudlMethod[] Methods { get; }
    ITypeHelper ResponseType { get; }
    ITypeDigger ResponseTypeDigger { get; }
    ICrudlMethod? ReadMethod { get; }
    ICrudlMethod? CreateMethod { get; }
    ICrudlMethod? UpdateMethod { get; }
    ICrudlMethod? DeleteMethod { get; }
    ICrudlMethod? ListMethod { get; }
    ICrudlType? JunctionLeftApi { get; }
    ICrudlType? JunctionRightApi { get; }
    string? IsPageRoute { get; }
    string? IsPageTitle { get; }
    string? IsPageSubmitText { get; }
    string? IsPageResponseText { get; }
    bool IsNotAuthorized { get; }
}
