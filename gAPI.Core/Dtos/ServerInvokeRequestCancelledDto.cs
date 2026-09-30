using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerInvokeRequestCancelledDto(
    ServerRoutingDto Routing,
    string? Reason);
