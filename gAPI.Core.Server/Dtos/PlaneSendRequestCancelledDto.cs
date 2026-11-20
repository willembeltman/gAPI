using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneSendRequestCancelledDto(
    PlaneRoutingDto Routing,
    string? Reason);