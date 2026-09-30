using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerInvokeRequestDoneDto(
    ServerRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);