using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeRequestClientDto(
    RoutingDto Routing,
    byte[] BinaryData,
    bool StateIsChanged,
    string? StateData)
    : InvokeRequestDto(Routing, BinaryData);
