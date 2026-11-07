using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Ids;

public static class FabricConnectionIdComparer
{
    [IsComparer]
    public static bool IsDifferent(this FabricConnectionId value, FabricConnectionId otherValue)
    {
        if (value.Value != otherValue.Value) return true;
        return false;
    }
}