using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusInvokeRequestAckDto(
    BusRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);
