using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestCancelledDto(
    RoutingDto Routing,
    string? Reason);
