using System.Collections.Generic;

namespace gAPI.AutoComponent.Interfaces;

public interface ICrudType : ISharedReference
{
    bool IsUser { get; }
    bool IsAuthorized { get; }
    bool HasIStorageFileDtoInterface { get; }
    bool HasIReadonlyStorageFileDtoInterface { get; }
    bool IsICrudEntity { get; }
    bool IsEntryPoint { get; }
    bool IsJunction { get; }
    ICrudProperty KeyProperty { get; }
    IEnumerable<ICrudProperty> Properties { get; }
    IEnumerable<ICrudProperty> ForeignItemProperties { get; }
    ICrudMethod[] Methods { get; }
    ITypeHelper ResponseType { get; }
    ITypeDigger ResponseTypeDigger { get; }
    ICrudMethod? ReadMethod { get; }
    ICrudMethod? CreateMethod { get; }
    ICrudMethod? UpdateMethod { get; }
    ICrudMethod? DeleteMethod { get; }
    ICrudMethod? ListMethod { get; }
    ICrudType? JunctionLeftApi { get; }
    ICrudType? JunctionRightApi { get; }
    string? IsPageRoute { get; }
    string? IsPageTitle { get; }
    string? IsPageSubmitText { get; }
    string? IsPageResponseText { get; }
    bool IsNotAuthorized { get; }
    bool HasStorageFileUrlProperty { get; }
}
