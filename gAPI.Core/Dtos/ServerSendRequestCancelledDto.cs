using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerSendRequestCancelledDto(
    ServerRoutingDto Routing,
    string? Reason);
