using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SynchronizeClientIdsDtoCreateCopy
{
    [IsCreateCopy]
    public static SynchronizeClientIdsDto CreateCopy(this SynchronizeClientIdsDto value)
    {
        return new SynchronizeClientIdsDto(value.FabricManagerId, value.FabricConnectionId, value.ClientConnectionId);
    }
}