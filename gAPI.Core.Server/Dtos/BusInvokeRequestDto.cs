using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusInvokeRequestDto(
    BusRoutingDto Routing,
    byte[] BinaryData);
