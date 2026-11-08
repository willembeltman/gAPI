using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusSendRequestCancelledDto(
    BusRoutingDto Routing,
    string? Reason);