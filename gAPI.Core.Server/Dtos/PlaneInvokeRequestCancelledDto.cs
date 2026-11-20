using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneInvokeRequestCancelledDto(
    PlaneRoutingDto Routing,
    string? Reason);
