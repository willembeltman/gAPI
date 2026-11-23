using gAPI.Core.Attributes;
using gAPI.Core.Dtos;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusInvokeRequestAckDto(
    BusRoutingDto Routing,
    bool EnumeratorCreated,
    bool Cancelled,
    ExceptionDto? Exception);
