using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestClientDto(
    RoutingDto Routing,
    bool StateIsChanged,
    string? StateData,
    byte[] BinaryData) 
    : SendRequestDto(Routing, BinaryData);
