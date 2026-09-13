using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestClientDto(
    RoutingDto Routing,
    byte[] BinaryData,
    bool StateIsChanged,
    string? StateData) 
    : SendRequestDto(Routing, BinaryData);
