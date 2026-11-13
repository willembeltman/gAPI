using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestAckClientDto(
    RoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestAckDto(Routing, Cancelled, ExceptionMessage);