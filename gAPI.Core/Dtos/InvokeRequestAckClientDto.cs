using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestAckClientDto(
    RoutingDto Routing,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestAckDto(Routing);