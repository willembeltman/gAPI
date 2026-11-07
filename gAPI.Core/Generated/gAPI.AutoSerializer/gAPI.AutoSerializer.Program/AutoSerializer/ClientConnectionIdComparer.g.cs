using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Ids;

public static class ClientConnectionIdComparer
{
    [IsComparer]
    public static bool IsDifferent(this ClientConnectionId value, ClientConnectionId otherValue)
    {
        if (value.Value != otherValue.Value) return true;
        return false;
    }
}