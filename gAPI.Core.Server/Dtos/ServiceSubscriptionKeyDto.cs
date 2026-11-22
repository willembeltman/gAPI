using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

public record ServiceSubscriptionKeyDto(
    ServiceId ServiceId, 
    ServiceSubscriptionId ServiceSubscriptionId);
