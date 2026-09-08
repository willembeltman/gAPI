using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestClientDto(
    RoutingDto Routing,
    bool StateIsChanged,
    string? StateData,
    byte[] BinaryData)
    : InvokeRequestDto(Routing, BinaryData);
