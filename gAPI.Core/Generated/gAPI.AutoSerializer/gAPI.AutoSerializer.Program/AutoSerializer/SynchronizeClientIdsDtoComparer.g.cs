using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SynchronizeClientIdsDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SynchronizeClientIdsDto value, SynchronizeClientIdsDto otherValue)
    {
        if (value.FabricManagerId != otherValue.FabricManagerId) return true;
        if (value.FabricConnectionId != otherValue.FabricConnectionId) return true;
        if (value.ClientConnectionId != otherValue.ClientConnectionId) return true;
        return false;
    }
}