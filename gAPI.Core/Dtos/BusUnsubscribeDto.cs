using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusUnsubscribeDto(
    ServiceId ServiceId,
    BusConnectionId BusConnectionId)
{
    public override string ToString()
    {
        // This string is a key
        return $"{ServiceId} #{BusConnectionId})";
    }
}