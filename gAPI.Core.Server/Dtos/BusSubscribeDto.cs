using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusSubscribeDto(
    ServiceSubscriptionId ServiceSubscriptionId,
    ServiceId ServiceId,
    BusConnectionId BusConnectionId)
{
    public override string ToString()
    {
        return $"{ServiceId} #{BusConnectionId}";
    }
}