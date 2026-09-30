using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record ServerStreamingRequestDto(
    ServerRoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool Cancelled);