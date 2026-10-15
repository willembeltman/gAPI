using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusSendRequestDto(
    BusRoutingDto Routing,
    byte[] BinaryData);