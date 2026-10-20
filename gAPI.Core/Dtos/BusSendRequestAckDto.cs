using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusSendRequestAckDto(
    BusRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);