using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneStreamingResponseDto(
    PlaneRoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool IsCompleted,
    bool IsCancelled,
    string? ExceptionMessage,
    string? InnerExceptionMessage,
    byte[] BinaryData);
