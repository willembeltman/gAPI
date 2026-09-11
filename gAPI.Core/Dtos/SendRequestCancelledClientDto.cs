using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestCancelledClientDto(
    RoutingDto Routing,
    string? Reason,
    bool StateIsChanged,
    string? StateData)
    : SendRequestCancelledDto(Routing, Reason);