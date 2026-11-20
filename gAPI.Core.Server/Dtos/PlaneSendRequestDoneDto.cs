using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneSendRequestDoneDto(
    PlaneRoutingDto Routing,
    bool Cancelled,
    string? ExceptionMessage,
    string? InnerExceptionMessage);