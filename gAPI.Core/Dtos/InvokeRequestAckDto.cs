using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestAckDto(
    RoutingDto Routing,
    bool EnumeratorCreated,
    bool Cancelled,
    ExceptionDto? Exception);