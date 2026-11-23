using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestAckClientDto(
    RoutingDto Routing,
    bool EnumeratorCreated,
    bool Cancelled,
    ExceptionDto? Exception,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestAckDto(Routing, EnumeratorCreated, Cancelled, Exception);