using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestCancelledDto(
    RoutingDto Routing,
    string? Reason);
