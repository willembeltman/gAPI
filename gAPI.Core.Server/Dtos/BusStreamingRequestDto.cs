using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusStreamingRequestDto(
    BusRoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool Cancelled);
