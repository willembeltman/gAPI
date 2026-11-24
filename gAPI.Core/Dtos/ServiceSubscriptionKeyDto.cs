using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

public record ServiceSubscriptionKeyDto(
    ServiceId ServiceId, 
    ServiceSubscriptionId ServiceSubscriptionId);
