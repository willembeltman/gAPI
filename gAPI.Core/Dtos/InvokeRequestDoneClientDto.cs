using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestDoneClientDto(
    RoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage,
    string? InnerExceptionMessage,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestDoneDto(Routing, Cancelled, ExceptionMessage, InnerExceptionMessage);