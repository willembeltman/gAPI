using gAPI.Core.Attributes;
using gAPI.Core.Enums;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusRoutingDto(
    BusConnectionId FromBusConnectionId,
    RequestId RequestId,
    ServiceId ServiceId,
    ServiceMethodId MethodId,
    BusToChoice To,
    BusConnectionId? ToBusConnectionId = null
    );
