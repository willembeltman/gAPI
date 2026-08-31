using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InitializeDtoCreateCopy
{
    [IsCreateCopy]
    public static InitializeDto CreateCopy(this InitializeDto value)
    {
        var copy = new InitializeDto();
        copy.StateData = value.StateData;
        return copy;
    }
}