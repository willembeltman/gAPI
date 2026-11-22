using gAPI.Core.Attributes;
using gAPI.Core.Dtos;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneInvokeRequestDoneDto(
    PlaneRoutingDto Routing,
    bool Cancelled,
    ExceptionDto? Exception);
