using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestCancelledClientDto(
    RoutingDto Routing,
    string? Reason,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestCancelledDto(Routing, Reason);