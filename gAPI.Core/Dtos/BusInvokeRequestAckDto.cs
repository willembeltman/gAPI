using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusInvokeRequestAckDto(
    BusRoutingDto Routing,
    string? Exception);
