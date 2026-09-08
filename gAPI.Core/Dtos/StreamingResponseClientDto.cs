using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record StreamingResponseClientDto(
    RoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool IsCompleted,
    bool StateIsChanged,
    string? StateData,
    byte[] BinaryData)
    : StreamingResponseDto(Routing, ArgumentIndex, StreamId, IsCompleted, BinaryData);
