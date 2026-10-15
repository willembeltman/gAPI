using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record BusStreamingResponseDto(
    BusRoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool IsCompleted,
    bool IsCancelled,
    string? ExceptionMessage,
    byte[] BinaryData);
