using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusSendRequestDoneDto(
    BusRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);