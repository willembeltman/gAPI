using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestDoneClientDto(
    RoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage,
    string? InnerExceptionMessage,
    bool StateIsChanged,
    string? StateData)
    : SendRequestDoneDto(Routing, Cancelled, ExceptionMessage, InnerExceptionMessage);