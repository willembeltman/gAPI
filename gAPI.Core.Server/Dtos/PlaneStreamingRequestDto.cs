using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneStreamingRequestDto(
    PlaneRoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool Cancelled);
