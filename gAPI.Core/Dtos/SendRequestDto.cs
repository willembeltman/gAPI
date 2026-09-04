using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestDto(
    RoutingDto Routing,
    bool StateIsChanged,
    string? StateData,
    byte[] BinaryData);