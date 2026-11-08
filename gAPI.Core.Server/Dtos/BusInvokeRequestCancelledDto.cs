using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusInvokeRequestCancelledDto(
    BusRoutingDto Routing,
    string? Reason);
