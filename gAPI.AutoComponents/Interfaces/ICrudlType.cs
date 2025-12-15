using System.Collections.Generic;

namespace gAPI.AutoComponents.Interfaces;

public interface ICrudlType : ISharedReference
{
    bool IsUser { get; }
    bool IsAuthorized { get; }
    bool IsStorageFile { get; }
    bool IsICrudEntity { get; }
    ICrudlProperty KeyProperty { get; }
    IEnumerable<ICrudlProperty> Properties { get; }
    IEnumerable<ICrudlProperty> ForeignItemProperties { get; }
    ICrudlMethod[] Methods { get; }
    ITypeHelper ResponseTypeHelper { get; }
    ITypeDigger ResponseTypeDigger { get; }
    ICrudlMethod? ReadMethod { get; }
    ICrudlMethod? CreateMethod { get; }
    ICrudlMethod? UpdateMethod { get; }
    ICrudlMethod? DeleteMethod { get; }
    ICrudlMethod? ListMethod { get; }
    ICrudlType? JunctionLeftApi { get; }
    ICrudlType? JunctionRightApi { get; }
}
