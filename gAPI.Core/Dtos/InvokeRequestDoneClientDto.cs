using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestDoneClientDto(
    RoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestDoneDto(Routing, Cancelled, ExceptionMessage);