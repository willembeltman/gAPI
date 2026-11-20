using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneInvokeRequestDto(
    PlaneRoutingDto Routing,
    byte[] BinaryData);
