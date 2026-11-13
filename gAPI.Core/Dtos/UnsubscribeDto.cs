using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record UnsubscribeDto(
    ServiceSubscriptionId ServiceSubscriptionId,
    ServiceId ServiceId,
    UserId UserId,
    SessionId SessionId)
{
    public override string ToString()
    {
        // This string is a key
        return $"{ServiceId} #{SessionId} ({UserId})";
    }
}