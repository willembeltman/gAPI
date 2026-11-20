using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneSubscribeDto(
    ServiceSubscriptionId ServiceSubscriptionId,
    ServiceId ServiceId,
    PlaneConnectionId PlaneConnectionId)
{
    public override string ToString()
    {
        return $"{ServiceId} #{PlaneConnectionId}";
    }
}