using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneUnsubscribeDto(
    ServiceSubscriptionId ServiceSubscriptionId,
    ServiceId ServiceId,
    PlaneConnectionId PlaneConnectionId)
{
    public override string ToString()
    {
        // This string is a key
        return $"{ServiceId} #{PlaneConnectionId})";
    }
}