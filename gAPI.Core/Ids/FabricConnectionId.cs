using gAPI.Core.Attributes;

namespace gAPI.Core.Ids;

[GenerateSerializer]
public record FabricConnectionId(long Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}