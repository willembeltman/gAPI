using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SynchronizeFabricIdsDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SynchronizeFabricIdsDto value, SynchronizeFabricIdsDto otherValue)
    {
        if (value.FabricManagerId != otherValue.FabricManagerId) return true;
        if (value.FabricConnectionId != otherValue.FabricConnectionId) return true;
        return false;
    }
}