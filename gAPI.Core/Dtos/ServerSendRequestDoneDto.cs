using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerSendRequestDoneDto(
    ServerRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);