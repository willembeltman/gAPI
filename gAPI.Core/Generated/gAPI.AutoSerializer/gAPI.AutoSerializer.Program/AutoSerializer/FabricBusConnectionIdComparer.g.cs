using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Ids;

public static class FabricBusConnectionIdComparer
{
    [IsComparer]
    public static bool IsDifferent(this FabricBusConnectionId value, FabricBusConnectionId otherValue)
    {
        if (value.Value != otherValue.Value) return true;
        return false;
    }
}