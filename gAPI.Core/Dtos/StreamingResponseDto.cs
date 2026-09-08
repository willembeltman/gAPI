using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record StreamingResponseDto(
    RoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool IsCompleted,
    byte[] BinaryData);
