using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestCancelledClientDto(
    RoutingDto Routing,
    bool StateIsChanged,
    string? StateData,
    string? Reason)
    : SendRequestCancelledDto(Routing, Reason);