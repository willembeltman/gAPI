using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record StreamingRequestClientDto(
    RoutingDto Routing,
    int ArgumentIndex,
    StreamId StreamId,
    bool Cancelled,
    bool StateIsChanged,
    string? StateData) 
    : StreamingRequestDto(Routing, ArgumentIndex, StreamId, Cancelled);
