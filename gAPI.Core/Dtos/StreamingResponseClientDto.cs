using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record StreamingResponseClientDto(
    RoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool IsCompleted,
    bool Cancelled,
    string? ExceptionMessage,
    byte[] BinaryData,
    bool StateIsChanged,
    string? StateData)
    : StreamingResponseDto(Routing, ArgumentIndex, StreamId, IsCompleted, Cancelled, ExceptionMessage, BinaryData);
