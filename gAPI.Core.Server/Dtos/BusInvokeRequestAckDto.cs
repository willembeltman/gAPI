using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusInvokeRequestAckDto(
    BusRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);
