using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestDoneDto(
    RoutingDto Routing,
    bool Cancelled,
    ExceptionDto? Exception)
    : InvokeRequestAckDto(Routing, Cancelled, Exception);