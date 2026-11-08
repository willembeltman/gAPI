using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusInvokeRequestDoneDto(
    BusRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);
