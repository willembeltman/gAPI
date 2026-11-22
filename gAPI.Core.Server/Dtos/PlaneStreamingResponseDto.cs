using gAPI.Core.Attributes;
using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneStreamingResponseDto(
    PlaneRoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool IsCompleted,
    bool IsCancelled,
    ExceptionDto? Exception,
    byte[] BinaryData);
