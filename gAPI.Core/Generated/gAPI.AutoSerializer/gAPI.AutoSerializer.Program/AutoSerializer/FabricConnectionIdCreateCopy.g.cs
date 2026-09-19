using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Ids;

public static class FabricConnectionIdCreateCopy
{
    [IsCreateCopy]
    public static FabricConnectionId CreateCopy(this FabricConnectionId value)
    {
        return new FabricConnectionId(value.Value);
    }
}