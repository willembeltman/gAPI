using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SynchronizeBusFabricIdsDtoCreateCopy
{
    [IsCreateCopy]
    public static SynchronizeBusFabricIdsDto CreateCopy(this SynchronizeBusFabricIdsDto value)
    {
        return new SynchronizeBusFabricIdsDto(value.FabricManagerId, value.FabricConnectionId);
    }
}