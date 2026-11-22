using gAPI.Core.Attributes;
using gAPI.Core.Enums;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusRoutingDto(
    BusConnectionId FromBusConnectionId,
    RequestId RequestId,
    ServiceId ServiceId,
    ServiceMethodId MethodId,
    TimeSpan Timeout,
    DeliveryMode deliveryMode,
    BusToChoice To,
    BusConnectionId? ToBusConnection = null,
    bool ThrowIfNoReceiver = true
    );
