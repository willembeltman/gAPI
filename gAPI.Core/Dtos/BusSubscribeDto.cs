using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusSubscribeDto(
    ServiceId ServiceId,
    BusConnectionId BusConnectionId)
{
    public override string ToString()
    {
        return $"{ServiceId} #{BusConnectionId}";
    }
}