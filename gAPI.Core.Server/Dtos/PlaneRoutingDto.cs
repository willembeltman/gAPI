using gAPI.Core.Attributes;
using gAPI.Core.Enums;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneRoutingDto(
    PlaneConnectionId FromPlaneConnectionId,
    RequestId RequestId,
    ServiceId ServiceId,
    ServiceMethodId MethodId,
    TimeSpan Timeout,
    DeliveryMode deliveryMode,
    PlaneToChoice To,
    PlaneConnectionId? ToPlaneConnectionId = null,
    ServiceSubscriptionId? ToServiceSubscriptionId = null,
    bool ThrowIfNoReceiver = true
    );
