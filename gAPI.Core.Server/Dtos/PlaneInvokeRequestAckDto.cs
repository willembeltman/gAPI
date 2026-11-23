using gAPI.Core.Attributes;
using gAPI.Core.Dtos;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneInvokeRequestAckDto(
    PlaneRoutingDto Routing,
    bool EnumeratorCreated,
    bool Cancelled,
    ExceptionDto? Exception);
