using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusInvokeRequestDto(
    BusRoutingDto Routing,
    byte[] BinaryData);
