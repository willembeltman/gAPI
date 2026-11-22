using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestDoneClientDto(
    RoutingDto Routing,
    bool Cancelled,
    ExceptionDto? Exception,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestDoneDto(Routing, Cancelled, Exception);