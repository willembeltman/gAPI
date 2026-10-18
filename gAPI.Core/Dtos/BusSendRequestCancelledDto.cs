using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusSendRequestCancelledDto(
    BusRoutingDto Routing,
    string? Reason);