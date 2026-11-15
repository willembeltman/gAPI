using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestDoneDto(
    RoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage,
    string? InnerExceptionMessage)
    : InvokeRequestAckDto(Routing, Cancelled, ExceptionMessage, InnerExceptionMessage);