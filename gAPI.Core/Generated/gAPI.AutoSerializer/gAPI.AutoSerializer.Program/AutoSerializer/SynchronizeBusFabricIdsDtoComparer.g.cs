using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SynchronizeBusFabricIdsDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SynchronizeBusFabricIdsDto value, SynchronizeBusFabricIdsDto otherValue)
    {
        if (value.FabricManagerId != otherValue.FabricManagerId) return true;
        if (value.FabricConnectionId != otherValue.FabricConnectionId) return true;
        return false;
    }
}