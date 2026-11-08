using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusSendRequestDto(
    BusRoutingDto Routing,
    byte[] BinaryData);