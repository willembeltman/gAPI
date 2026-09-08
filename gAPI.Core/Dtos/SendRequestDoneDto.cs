using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestDoneDto(
    RoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage);