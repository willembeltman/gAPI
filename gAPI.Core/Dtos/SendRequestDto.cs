using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestDto(
    RoutingDto Routing,
    byte[] BinaryData);