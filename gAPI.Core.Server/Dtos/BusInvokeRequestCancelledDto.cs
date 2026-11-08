using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusInvokeRequestCancelledDto(
    BusRoutingDto Routing,
    string? Reason);
