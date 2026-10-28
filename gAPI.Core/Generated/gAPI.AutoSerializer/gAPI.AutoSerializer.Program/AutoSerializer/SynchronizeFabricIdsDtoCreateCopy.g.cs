using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SynchronizeFabricIdsDtoCreateCopy
{
    [IsCreateCopy]
    public static SynchronizeFabricIdsDto CreateCopy(this SynchronizeFabricIdsDto value)
    {
        return new SynchronizeFabricIdsDto(value.FabricManagerId, value.FabricConnectionId);
    }
}