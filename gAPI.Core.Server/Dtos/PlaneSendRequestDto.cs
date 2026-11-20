using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneSendRequestDto(
    PlaneRoutingDto Routing,
    byte[] BinaryData);