using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record StreamingRequestDto(
    RoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId);
