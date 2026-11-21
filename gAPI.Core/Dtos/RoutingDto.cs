using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record RoutingDto(
    FabricManagerId From_FabricManagerId,
    FabricConnectionId From_FabricConnectionId,

    RequestId RequestId,
    ServiceId ServiceId,
    ServiceMethodId MethodId,
    bool IsAuthenticated,
    bool IsNotAuthenticated,

    TimeSpan Timeout,

    UserId? UserId,
    SessionId? SessionId,

    bool ThrowIfNoReceiver = true
    );
