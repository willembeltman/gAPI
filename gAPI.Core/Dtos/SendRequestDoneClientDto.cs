using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestDoneClientDto(
    RoutingDto Routing,
    bool Cancelled,
    ExceptionDto? Exception,
    bool StateIsChanged,
    string? StateData)
    : SendRequestDoneDto(Routing, Cancelled, Exception);